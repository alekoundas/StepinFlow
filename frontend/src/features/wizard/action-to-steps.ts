import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import { cursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import { DraftStepSourceEnum } from "@/shared/enums/backend/draft-step-source-enum";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";

/**
 * What a recorded action is allowed to become, and how it becomes it.
 *
 * The recorder deliberately stops at the action, because a click could reasonably be a cursor
 * click, an image search, or both, and only the person who did it knows which. This is the one
 * place that turns an answer into steps.
 */
export interface ActionOption {
  id: string;
  label: string;
  description: string;
  iconName: string;

  /** How many steps this produces, shown so the tree growing by two is not a surprise. */
  stepCount: number;

  /** Some options only make sense when the recorder managed to capture the screen. */
  requiresScreenshot?: boolean;
}

export interface Placement {
  parentTempId?: number;
  parentBranch?: FlowStepTypeEnum;
}

/**
 * What the wizard asks about an action, in the words of the step rather than the recording.
 *
 * Kept separate from the steps because one answer can belong to a different step than another:
 * a recorded click becomes a move and a press, and the button belongs to the press while the
 * search area belongs to neither.
 */
export interface ActionAnswers {
  name: string;
  cursorButtonType?: CursorButtonTypeEnum;
  cursorButtonActionType?: CursorButtonActionTypeEnum;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum;
  loopCount: number;
  keyboardInputText: string;
  waitForMilliseconds: number;
  timeoutMilliseconds: number;
  flowAreaId?: number;

  /** Base64 PNG cropped from the recorded screenshot. */
  template?: string;
}

/** What the recording already knows, before the user changes anything. */
export const defaultAnswers = (
  action: RecordedActionDto,
  optionId: string,
): ActionAnswers => ({
  name: defaultName(action, optionId),
  cursorButtonType: action.cursorButtonType ?? undefined,
  cursorButtonActionType:
    action.cursorButtonActionType ?? cursorButtonActionTypeEnum.SINGLE_CLICK,
  cursorScrollDirectionType: action.scrollDirection ?? undefined,
  loopCount: action.scrollAmount,
  keyboardInputText: action.text ?? "",
  waitForMilliseconds: action.pauseMilliseconds,
  timeoutMilliseconds: Math.max(action.pauseMilliseconds * 3, 5000),
});

const defaultName = (action: RecordedActionDto, optionId: string): string => {
  switch (optionId) {
    case "click":
      return "Click";
    case "image-click":
    case "image-only":
      return "Find on screen";
    case "wait-for-image":
      return "Wait for it to appear";
    case "drag":
      return "Drag";
    case "scroll":
      return `Scroll ${action.scrollDirection?.toLowerCase() ?? ""}`.trim();
    case "type-text":
      return "Type text";
    case "send-keys":
      return `Press ${action.text ?? ""}`.trim();
    default:
      return "Wait";
  }
};

const CLICK_OPTIONS: ActionOption[] = [
  {
    id: "click",
    label: "Click at this position",
    description: "Moves to the exact coordinates and clicks. Breaks if the window moves.",
    iconName: "mouse",
    stepCount: 2,
  },
  {
    id: "image-click",
    label: "Find this image, then click it",
    description: "Looks for what you clicked and clicks wherever it is now.",
    iconName: "search",
    stepCount: 3,
    requiresScreenshot: true,
  },
  {
    id: "image-only",
    label: "Only check it is on screen",
    description: "Branches on whether it is there, without clicking.",
    iconName: "eye",
    stepCount: 1,
    requiresScreenshot: true,
  },
];

const TYPING_OPTIONS: ActionOption[] = [
  {
    id: "type-text",
    label: "Type this text",
    description: "Sends the characters as typed.",
    iconName: "keyboard",
    stepCount: 1,
  },
  {
    id: "send-keys",
    label: "Send as key presses",
    description: "For shortcuts rather than words.",
    iconName: "bolt",
    stepCount: 1,
  },
];

const PAUSE_OPTIONS: ActionOption[] = [
  {
    id: "wait",
    label: "Wait this long",
    description: "Pauses for the time you paused for.",
    iconName: "clock",
    stepCount: 1,
  },
  {
    id: "wait-for-image",
    label: "Wait until something appears",
    description: "Polls instead of guessing a duration. You add the template next.",
    iconName: "hourglass",
    stepCount: 1,
  },
];

export const optionsFor = (action: RecordedActionDto): ActionOption[] => {
  switch (action.kind) {
    case RecordedActionKindEnum.CLICK:
      return CLICK_OPTIONS.filter(
        (x) => !x.requiresScreenshot || action.screenshotIndex != null,
      );

    case RecordedActionKindEnum.TYPING:
      return TYPING_OPTIONS;

    case RecordedActionKindEnum.KEY_COMBINATION:
      return [TYPING_OPTIONS[1], TYPING_OPTIONS[0]];

    case RecordedActionKindEnum.PAUSE:
      return PAUSE_OPTIONS;

    case RecordedActionKindEnum.DRAG:
      return [
        {
          id: "drag",
          label: "Drag between these points",
          description: "Presses at the start, releases at the end.",
          iconName: "arrows-alt",
          stepCount: 1,
        },
      ];

    case RecordedActionKindEnum.SCROLL:
      return [
        {
          id: "scroll",
          label: "Scroll here",
          description: "Moves to this spot and repeats the same wheel movement.",
          iconName: "sort-alt",
          stepCount: 2,
        },
      ];

    default:
      return [];
  }
};

/**
 * Turns one answered action into the steps it stands for, numbered from nextTempId.
 *
 * Anything that happens somewhere takes two steps, because a click and a scroll act wherever
 * the cursor already is: only Cursor Move carries a position. So a recorded click is a move
 * followed by a press, and finding an image is a search, a move onto what it found, and a press.
 *
 * The move points at the search by temp id rather than by position. The save swaps in the real
 * id once both rows exist, and from then on the cursor follows whatever the search finds.
 */
export const buildSteps = (
  action: RecordedActionDto,
  optionId: string,
  placement: Placement,
  nextTempId: number,
  answers: ActionAnswers,
): DraftStepDto[] => {
  let tempId = nextTempId;

  const base = (values: Partial<FlowStepDto>): DraftStepDto => ({
    tempId: tempId++,
    parentTempId: placement.parentTempId,
    parentBranch: placement.parentBranch,
    values: new FlowStepDto(values),
    unresolved: [],
    source: DraftStepSourceEnum.RECORDING,
    evidence: {
      screenshotIndex: action.screenshotIndex,
      windowTitle: action.windowTitle,
      summary: action.summary,
    },
  });

  const pointName = `${action.locationX}, ${action.locationY}`;

  /** A press happens wherever the cursor was left, so it carries no position. */
  const press = (): DraftStepDto =>
    base({
      flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
      name: answers.name,
      cursorButtonType: answers.cursorButtonType,
      cursorButtonActionType: answers.cursorButtonActionType,
    });

  /** The search half of every image option, which is where the template and area live. */
  const search = (searchMode: SearchModeEnum, timeoutMilliseconds = 0): DraftStepDto =>
    base({
      flowStepType: FlowStepTypeEnum.IMAGE_SEARCH,
      name: answers.name,
      searchMode,
      timeoutMilliseconds,
      flowAreaId: answers.flowAreaId,
      flowStepImages: answers.template
        ? [new FlowStepImageDto({ name: "Recorded template", templateImage: answers.template })]
        : [],
    });

  switch (optionId) {
    case "click": {
      const move: DraftStepDto = {
        ...base({
          flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
          name: "Move to position",
          
        }),
        newPoint: {
          name: `Click point ${pointName}`,
          locationX: action.locationX,
          locationY: action.locationY,
        },
      };

      return [move, press()];
    }

    case "image-only":
      return [search(SearchModeEnum.FIND_BEST)];

    case "image-click": {
      const found = search(SearchModeEnum.FIND_BEST);

      // Both sit in the search Success branch: there is nothing to move onto until it has
      // actually found something.
      const inSuccess = {
        parentTempId: found.tempId,
        parentBranch: FlowStepTypeEnum.SUCCESS,
      };

      const move: DraftStepDto = {
        ...base({
          flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
          name: "Move onto it",
          // The search result is the position, so there is no point to create.
          
        }),
        ...inSuccess,
        referenceTempId: found.tempId,
      };

      const click: DraftStepDto = { ...press(), ...inSuccess };
      click.values.name = "Click it";

      return [found, move, click];
    }

    case "drag":
      return [
        {
          ...base({
            flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
            name: answers.name,
            
            
            cursorButtonType: answers.cursorButtonType,
          }),
          newPoint: {
            name: `Drag from ${pointName}`,
            locationX: action.locationX,
            locationY: action.locationY,
          },
          newPointEnd: {
            name: `Drag to ${action.locationEndX}, ${action.locationEndY}`,
            locationX: action.locationEndX,
            locationY: action.locationEndY,
          },
        },
      ];

    case "scroll": {
      // A wheel turn has no position of its own either, so it needs moving to first.
      const move: DraftStepDto = {
        ...base({
          flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
          name: "Move to position",
          
        }),
        newPoint: {
          name: `Scroll point ${pointName}`,
          locationX: action.locationX,
          locationY: action.locationY,
        },
      };

      const scroll = base({
        flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
        name: answers.name,
        cursorScrollDirectionType: answers.cursorScrollDirectionType,
        loopCount: answers.loopCount,
      });

      return [move, scroll];
    }

    case "type-text":
      return [
        base({
          flowStepType: FlowStepTypeEnum.KEYBOARD_INPUT,
          name: answers.name,
          keyboardInputType: KeyboardInputTypeEnum.TEXT,
          keyboardInputText: answers.keyboardInputText,
        }),
      ];

    case "send-keys":
      return [
        base({
          flowStepType: FlowStepTypeEnum.KEYBOARD_INPUT,
          name: answers.name,
          keyboardInputType: KeyboardInputTypeEnum.COMBINATION,
          keyboardInputText: answers.keyboardInputText,
        }),
      ];

    case "wait":
      return [
        base({
          flowStepType: FlowStepTypeEnum.WAIT,
          name: answers.name,
          waitForMilliseconds: answers.waitForMilliseconds,
        }),
      ];

    case "wait-for-image":
      return [search(SearchModeEnum.WAIT_UNTIL_FOUND, answers.timeoutMilliseconds)];

    default:
      return [];
  }
};
