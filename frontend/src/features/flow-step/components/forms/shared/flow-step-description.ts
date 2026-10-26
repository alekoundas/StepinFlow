import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { cursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KeyboardKeyActionTypeEnum } from "@/shared/enums/backend/keyboard-key-action-type-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepScriptLineDto } from "@/shared/models/flow-step-script-line-dto";
import { getFlowStepCatalogEntry } from "@/shared/models/flow-step-catalog";
import { humanizeMilliseconds } from "@/shared/utils/humanize-milliseconds";

import { CONDITION_LABELS } from "@/features/flow-step/components/forms/shared/condition-types";
import {
  SEARCH_IMAGE_MODES,
  SEARCH_TEXT_MODES,
} from "@/features/flow-step/components/forms/shared/search-modes";
import { CURSOR_MODES } from "@/features/flow-step/components/forms/cursor/cursor-modes";
import { WINDOW_MODES } from "@/features/flow-step/components/forms/window/window-modes";
import { SYSTEM_ACTIONS } from "@/features/flow-step/components/forms/system-action/system-actions";
import {
  KEY_ACTIONS,
  KEYBOARD_MODES,
} from "@/features/flow-step/components/forms/keyboard/keyboard-modes";

/**
 * What a step's form says about it above and below its fields: the type and mode in the header,
 * a sentence of what it will do, and what later steps can use from it. Read from the values on
 * the form, so it follows every change; the names of what it points at come with its script line,
 * since the form holds only their ids.
 */
export interface FlowStepDescription {
  type: string;
  mode?: string;
  summary?: string;
  gives?: string;
}

type Names = Partial<FlowStepScriptLineDto> | undefined;

const CURSOR_TYPES: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.CURSOR_CLICK,
  FlowStepTypeEnum.CURSOR_RELOCATE,
  FlowStepTypeEnum.CURSOR_DRAG,
  FlowStepTypeEnum.CURSOR_SCROLL,
];

const WINDOW_TYPES: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.WINDOW_FOCUS,
  FlowStepTypeEnum.WINDOW_RESIZE,
  FlowStepTypeEnum.WINDOW_RELOCATE,
];

const BUTTON_NAMES: Record<string, string> = {
  [CursorButtonTypeEnum.LEFT_BUTTON]: "left",
  [CursorButtonTypeEnum.RIGHT_BUTTON]: "right",
  [CursorButtonTypeEnum.MIDDLE_BUTTON]: "middle",
};

const quoted = (text: string | null | undefined) => `“${text ?? ""}”`;

const plural = (count: number, one: string, many: string) =>
  `${count} ${count === 1 ? one : many}`;

// Up to a time, or for as long as it takes when there is none.
const forUpTo = (timeout: number) =>
  timeout > 0 ? `for up to ${humanizeMilliseconds(timeout)}` : "for as long as it takes";

export const conditionPhrase = (dto: FlowStepDto): string => {
  const condition = dto.conditionType ?? ConditionTypeEnum.CONTAINS;
  const label = CONDITION_LABELS[condition].toLowerCase();

  if (condition === ConditionTypeEnum.IS_EMPTY || condition === ConditionTypeEnum.IS_NOT_EMPTY)
    return label;

  if (condition === ConditionTypeEnum.BETWEEN)
    return `${label} ${quoted(dto.conditionText)} and ${quoted(dto.conditionTextEnd)}`;

  return `${label} ${quoted(dto.conditionText)}`;
};

const windowPhrase = (names: Names): string =>
  names?.areaName ? `the ${quoted(names.areaName)} window` : "the window";

const typeLabel = (type: FlowStepTypeEnum | undefined): string => {
  if (type && CURSOR_TYPES.includes(type)) return "Cursor";
  if (type && WINDOW_TYPES.includes(type)) return "Window";
  if (type === FlowStepTypeEnum.SUCCESS || type === FlowStepTypeEnum.FAILURE) return "Branch";

  return getFlowStepCatalogEntry(type)?.label ?? "Step";
};

export function describeFlowStep(dto: FlowStepDto, names: Names): FlowStepDescription {
  const type = typeLabel(dto.flowStepType);
  const reference = names?.referenceName ? quoted(names.referenceName) : "the chosen step";
  const area = names?.areaName ? quoted(names.areaName) : "the area";

  switch (dto.flowStepType) {
    case FlowStepTypeEnum.SEARCH_IMAGE: {
      const count = dto.flowStepTemplates?.length ?? 0;
      const what = count === 0 ? "its template (none yet)" : plural(count, "template", "templates");
      const mode = SEARCH_IMAGE_MODES.find((x) => x.value === dto.searchMode)?.label;
      const where = names?.areaName ? ` in ${quoted(names.areaName)}` : "";

      if (dto.searchMode === SearchModeEnum.WAIT_UNTIL_NOT_FOUND) {
        return {
          type,
          mode,
          summary: `Waits until ${what} is no longer on screen${where}, checking every ${dto.pollIntervalMilliseconds} ms ${forUpTo(dto.timeoutMilliseconds)}. Then goes on to Success once; still there when time is up, to Failure.`,
        };
      }

      if (dto.searchMode === SearchModeEnum.WAIT_UNTIL_FOUND) {
        return {
          type,
          mode,
          summary: `Waits for ${what} to appear${where}, checking every ${dto.pollIntervalMilliseconds} ms ${forUpTo(dto.timeoutMilliseconds)}. On the first match it goes on to Success once; if nothing appears in time, to Failure.`,
          gives: "Where it matched: the location a Move or a Drag below can go to.",
        };
      }

      if (dto.searchMode === SearchModeEnum.FIND_ALL) {
        return {
          type,
          mode,
          summary: `Looks for ${what} once${where} and goes on to Success for every match, up to ${dto.maxMatches}. If nothing matches, to Failure.`,
          gives: `Inside Success, the match of the current pass: a Move to ${quoted(dto.name)} goes to each in turn.`,
        };
      }

      return {
        type,
        mode,
        summary: `Looks for ${what} once${where} and goes on to Success with the best match. If nothing matches, to Failure.`,
        gives: "Where it matched: the location a Move or a Drag below can go to.",
      };
    }

    case FlowStepTypeEnum.SEARCH_TEXT: {
      const mode = SEARCH_TEXT_MODES.find((x) => x.value === dto.searchMode)?.label;
      const condition = conditionPhrase(dto);
      const kept = dto.resultExtractPattern ? `, keeps what ${quoted(dto.resultExtractPattern)} captures,` : "";
      const gives = `What it read, as {{${dto.name}}}, for a Check Value or a Type below.`;

      if (dto.searchMode === SearchModeEnum.WAIT_UNTIL_NOT_FOUND) {
        return {
          type,
          mode,
          summary: `Reads ${area} every ${dto.pollIntervalMilliseconds} ms until the text no longer ${condition}, ${forUpTo(dto.timeoutMilliseconds)}.`,
        };
      }

      if (dto.searchMode === SearchModeEnum.WAIT_UNTIL_FOUND) {
        return {
          type,
          mode,
          summary: `Reads ${area} every ${dto.pollIntervalMilliseconds} ms${kept} until the text ${condition}, ${forUpTo(dto.timeoutMilliseconds)}.`,
          gives,
        };
      }

      return {
        type,
        mode,
        summary: `Reads ${area} once${kept} and goes on to Success when the text ${condition}.`,
        gives,
      };
    }

    case FlowStepTypeEnum.CHECK_VALUE:
      return {
        type,
        mode: dto.conditionType ? CONDITION_LABELS[dto.conditionType] : undefined,
        summary: `Goes on to Success when what ${reference} gave ${conditionPhrase(dto)}, to Failure otherwise.`,
      };

    case FlowStepTypeEnum.CURSOR_CLICK: {
      const button = BUTTON_NAMES[dto.cursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON];
      const mode = CURSOR_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label;

      switch (dto.cursorButtonActionType) {
        case cursorButtonActionTypeEnum.DOUBLE_CLICK:
          return { type, mode, summary: `A double ${button} click where the cursor is. The Move above it says where.` };
        case cursorButtonActionTypeEnum.HOLD_CLICK:
          return { type, mode, summary: `Holds the ${button} button down where the cursor is, until a Release.` };
        case cursorButtonActionTypeEnum.RELEASE_CLICK:
          return { type, mode, summary: `Lets go of the ${button} button.` };
        default:
          return { type, mode, summary: `A single ${button} click where the cursor is. The Move above it says where.` };
      }
    }

    case FlowStepTypeEnum.CURSOR_RELOCATE: {
      const mode = CURSOR_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label;

      if (names?.pointName)
        return { type, mode, summary: `Moves the cursor to the saved location ${quoted(names.pointName)}.` };

      return { type, mode, summary: `Moves the cursor onto where ${reference} matched.` };
    }

    case FlowStepTypeEnum.CURSOR_DRAG: {
      const mode = CURSOR_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label;
      const button = BUTTON_NAMES[dto.cursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON];
      const grab = names?.pointName
        ? `on ${quoted(names.pointName)}`
        : `where ${names?.referenceName ? quoted(names.referenceName) : "the chosen step"} matched`;
      const drop = names?.pointEndName
        ? `to ${quoted(names.pointEndName)}`
        : `to where ${names?.referenceEndName ? quoted(names.referenceEndName) : "the chosen step"} matched`;

      return { type, mode, summary: `Holds the ${button} button down ${grab}, drags ${drop}, and lets go.` };
    }

    case FlowStepTypeEnum.CURSOR_SCROLL: {
      const mode = CURSOR_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label;
      const direction = (dto.cursorScrollDirectionType ?? "DOWN").toLowerCase();

      return { type, mode, summary: `Turns the wheel ${plural(dto.loopCount, "notch", "notches")} ${direction} where the cursor is.` };
    }

    case FlowStepTypeEnum.KEYBOARD_INPUT: {
      if (dto.keyboardInputType !== KeyboardInputTypeEnum.COMBINATION) {
        return {
          type,
          mode: KEYBOARD_MODES[0].label,
          summary: `Types ${quoted(dto.keyboardInputText)} into whatever has focus, one character at a time.`,
        };
      }

      const action = dto.keyboardKeyActionType ?? KeyboardKeyActionTypeEnum.PRESS;
      const mode = `${KEYBOARD_MODES[1].label} · ${KEY_ACTIONS.find((x) => x.value === action)?.label}`;
      const keys = dto.keyboardInputText || "the keys";

      if (action === KeyboardKeyActionTypeEnum.HOLD)
        return { type, mode, summary: `Puts ${keys} down and leaves it down for the steps after it, until a Release.` };

      if (action === KeyboardKeyActionTypeEnum.RELEASE)
        return { type, mode, summary: `Lets go of the ${keys} a Hold above put down.` };

      return { type, mode, summary: `Presses ${keys} and lets go, the way a shortcut arrives.` };
    }

    case FlowStepTypeEnum.WAIT:
      if (dto.waitForMillisecondsMax > dto.waitForMilliseconds) {
        return {
          type,
          mode: "Random range",
          summary: `Each execution waits somewhere between ${humanizeMilliseconds(dto.waitForMilliseconds)} and ${humanizeMilliseconds(dto.waitForMillisecondsMax)}.`,
        };
      }

      return { type, mode: "Fixed", summary: `Waits ${humanizeMilliseconds(dto.waitForMilliseconds)}.` };

    case FlowStepTypeEnum.LOOP:
      if (dto.isLoopInfinite) {
        return {
          type,
          mode: "Forever",
          summary: "Executes the steps inside it again and again, until a Break or an End Execution inside it.",
        };
      }

      return { type, mode: "Times", summary: `Executes the steps inside it ${plural(dto.loopCount, "time", "times")}.` };

    case FlowStepTypeEnum.BREAK:
      return { type, summary: `Leaves ${reference} and carries on with what follows it. Nothing after this step inside it executes.` };

    case FlowStepTypeEnum.SUB_FLOW:
      return {
        type,
        summary: names?.subFlowName
          ? `Executes the ${quoted(names.subFlowName)} sub-flow, then comes back here.`
          : "Executes a sub-flow, then comes back here.",
      };

    case FlowStepTypeEnum.STAGE_MARKER:
      return {
        type,
        summary: `Starts the stage ${quoted(dto.name)}: the report groups the steps after it under that name, until the next marker.`,
      };

    case FlowStepTypeEnum.END_EXECUTION:
      if (dto.endExecutionAsSuccess) return { type, mode: "Passed", summary: "Stops here and reports a pass." };

      return {
        type,
        mode: "Failed",
        summary: dto.message
          ? `Stops here and reports a failure: ${quoted(dto.message)}.`
          : "Stops here and reports a failure.",
      };

    case FlowStepTypeEnum.SYSTEM_ACTION: {
      const action = SYSTEM_ACTIONS.find((x) => x.systemActionType === dto.systemActionType);

      return { type, mode: action?.label, summary: action?.description };
    }

    case FlowStepTypeEnum.NOTIFY: {
      const what = dto.message ? quoted(dto.message) : "the flow's name";
      const why = names?.referenceName
        ? `, with what ${quoted(names.referenceName)} was trying to do and why it failed`
        : "";

      return { type, summary: `Posts ${what} to Discord${why}.` };
    }

    case FlowStepTypeEnum.WINDOW_FOCUS:
      return {
        type,
        mode: WINDOW_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label,
        summary: `Brings ${windowPhrase(names)} to the front.`,
      };

    case FlowStepTypeEnum.WINDOW_RESIZE:
      return {
        type,
        mode: WINDOW_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label,
        summary: `Makes ${windowPhrase(names)} ${dto.windowWidth} × ${dto.windowHeight}.`,
      };

    case FlowStepTypeEnum.WINDOW_RELOCATE:
      return {
        type,
        mode: WINDOW_MODES.find((x) => x.flowStepType === dto.flowStepType)?.label,
        summary: names?.pointName
          ? `Moves ${windowPhrase(names)} so its corner lands on ${quoted(names.pointName)}.`
          : `Moves ${windowPhrase(names)} to a saved location.`,
      };

    case FlowStepTypeEnum.SYSTEM_COMMAND:
      return {
        type,
        gives: `What it printed, as {{${dto.name}}}, for a Check Value below.`,
      };

    default:
      return { type };
  }
}
