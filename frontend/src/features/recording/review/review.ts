import { RecordedWindowEnum } from "../../../../../electron/shared/types";
import { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";
import type { ClickGroupDto } from "@/shared/models/click-group-dto";
import type { RecordingResultDto } from "@/shared/models/recording-result-dto";
import type { ReviewedRecordingDto } from "@/shared/models/reviewed-recording-dto";

// A wait is three times what the tester waited, and never under 10 s, because the machine a flow
// executes on is often slower than the one it was recorded on; never over 60 s either.
const WAIT_FACTOR = 3;
const MINIMUM_WAIT_MILLISECONDS = 10_000;
const MAXIMUM_WAIT_MILLISECONDS = 60_000;

const SECRET_LABEL = /pass(word|code)?|\bpin\b|secret/i;

/** In the main area's pixels. */
export interface ElementRectangle {
  x: number;
  y: number;
  width: number;
  height: number;
}

/** What the tester has decided about one action so far. */
export interface ActionReview {
  choice: ReviewChoiceEnum;
  element: ElementRectangle | null;
  inputName: string;
  isSecret: boolean;
}

export interface ReviewOption {
  label: string;
  value: ReviewChoiceEnum;
}

/** What Review lists: everything the tester did. A pause is a wait, not something they did. */
export function listedActions(result: RecordingResultDto): RecordedActionDto[] {
  return result.actions.filter((x) => x.kind !== RecordedActionKindEnum.PAUSE);
}

export function isPointed(action: RecordedActionDto): boolean {
  return action.kind === RecordedActionKindEnum.CLICK || action.kind === RecordedActionKindEnum.DRAG;
}

export function isOutside(action: RecordedActionDto): boolean {
  return action.window === RecordedWindowEnum.OTHER;
}

export function elementOf(result: RecordingResultDto, action: RecordedActionDto): ClickedElementDto | undefined {
  return result.clickedElements.find((x) => x.actionIndex === action.index);
}

export function groupOf(result: RecordingResultDto, action: RecordedActionDto): ClickGroupDto | undefined {
  return result.clickGroups.find((x) => x.actionIndexes.includes(action.index));
}

export function optionsFor(action: RecordedActionDto, element: ClickedElementDto | undefined): ReviewOption[] {
  const leaveOut = { label: "Leave out", value: ReviewChoiceEnum.LEAVE_OUT };

  if (isPointed(action)) {
    const isDrag = action.kind === RecordedActionKindEnum.DRAG;
    const atSpot = {
      label: isDrag ? "Drag from this exact spot" : "Click this exact spot",
      value: ReviewChoiceEnum.CLICK_AT_SPOT,
    };

    if (isOutside(action)) return [leaveOut, atSpot];
    if (!element) return [atSpot, leaveOut];

    const waitFor = {
      label: isDrag ? "Wait for it, then drag" : "Wait for it, then click",
      value: ReviewChoiceEnum.WAIT_AND_CLICK,
    };
    return [waitFor, atSpot, leaveOut];
  }

  if (action.kind === RecordedActionKindEnum.TYPING)
    return [
      { label: "An input", value: ReviewChoiceEnum.INPUT },
      { label: "Type it as written", value: ReviewChoiceEnum.TYPE_AS_WRITTEN },
      leaveOut,
    ];

  if (action.kind === RecordedActionKindEnum.SCROLL)
    return [{ label: "Scroll", value: ReviewChoiceEnum.KEEP }, leaveOut];

  if (action.kind === RecordedActionKindEnum.KEY_HOLD)
    return [{ label: "Hold it", value: ReviewChoiceEnum.KEEP }, leaveOut];

  if (action.kind === RecordedActionKindEnum.KEY_RELEASE)
    return [{ label: "Release it", value: ReviewChoiceEnum.KEEP }, leaveOut];

  return [{ label: "Press it", value: ReviewChoiceEnum.KEEP }, leaveOut];
}

/** Each action's first option, with typed text named from the field it went into. */
export function initialReviews(result: RecordingResultDto): Record<number, ActionReview> {
  const reviews: Record<number, ActionReview> = {};
  const usedNames = new Set<string>();

  for (const action of listedActions(result)) {
    const element = elementOf(result, action);
    const label = fieldLabelBefore(result, action);

    let inputName = "";
    if (action.kind === RecordedActionKindEnum.TYPING) inputName = uniqueName(toInputName(label), usedNames);

    reviews[action.index] = {
      choice: optionsFor(action, element)[0].value,
      element: element ? { x: element.x, y: element.y, width: element.width, height: element.height } : null,
      inputName,
      isSecret: label !== null && SECRET_LABEL.test(label),
    };
  }

  return reviews;
}

/** The text on the element of the last click before it: the field typed text went into. */
export function fieldLabelBefore(result: RecordingResultDto, action: RecordedActionDto): string | null {
  const before = result.actions.filter((x) => x.index < action.index && isPointed(x));
  const last = before[before.length - 1];
  if (!last) return null;

  const text = elementOf(result, last)?.text ?? "";
  return text.length > 0 ? text : null;
}

function toInputName(label: string | null): string {
  const name = (label ?? "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");

  return name.length > 0 ? name : "input";
}

function uniqueName(name: string, used: Set<string>): string {
  let unique = name;
  for (let suffix = 2; used.has(unique); suffix++) unique = `${name}_${suffix}`;

  used.add(unique);
  return unique;
}

export function waitFor(waitedMilliseconds: number): number {
  const scaled = waitedMilliseconds * WAIT_FACTOR;
  return Math.min(MAXIMUM_WAIT_MILLISECONDS, Math.max(MINIMUM_WAIT_MILLISECONDS, scaled));
}

export function toSeconds(milliseconds: number): string {
  return `${(milliseconds / 1000).toFixed(1)} s`;
}

export function titleOf(action: RecordedActionDto, element: ClickedElementDto | undefined): string {
  const text = element?.text ?? "";

  if (action.kind === RecordedActionKindEnum.CLICK && text.length > 0) return `Clicked “${text}”`;
  if (action.kind === RecordedActionKindEnum.DRAG && text.length > 0) return `Dragged “${text}”`;

  if (action.kind === RecordedActionKindEnum.TYPING) {
    const count = [...(action.text ?? "")].length;
    return count === 1 ? "Typed 1 character" : `Typed ${count} characters`;
  }

  return action.summary;
}

export function subtitleOf(action: RecordedActionDto, review: ActionReview): string {
  if (review.choice === ReviewChoiceEnum.LEAVE_OUT && isOutside(action)) return "outside the window you picked";
  if (review.choice === ReviewChoiceEnum.LEAVE_OUT) return "left out";

  if (action.kind === RecordedActionKindEnum.TYPING) {
    if (review.choice === ReviewChoiceEnum.TYPE_AS_WRITTEN) return "typed as written";
    return `becomes the input ${review.inputName}${review.isSecret ? " · secret" : ""}`;
  }

  if (isPointed(action) && action.waitedMilliseconds > 0) return `after ${toSeconds(action.waitedMilliseconds)}`;
  return "";
}

/** The first option that keeps it, for a checkbox ticked back on. */
export function keepingChoice(options: ReviewOption[]): ReviewChoiceEnum {
  return options.find((x) => x.value !== ReviewChoiceEnum.LEAVE_OUT)?.value ?? ReviewChoiceEnum.KEEP;
}

export function toReviewed(result: RecordingResultDto, reviews: Record<number, ActionReview>): ReviewedRecordingDto {
  return {
    actions: listedActions(result).map((action) => {
      const review = reviews[action.index];
      const isInput = review.choice === ReviewChoiceEnum.INPUT;

      return {
        actionIndex: action.index,
        choice: review.choice,
        elementX: review.element?.x ?? null,
        elementY: review.element?.y ?? null,
        elementWidth: review.element?.width ?? null,
        elementHeight: review.element?.height ?? null,
        inputName: isInput ? review.inputName : null,
        isSecret: isInput && review.isSecret,
      };
    }),
  };
}
