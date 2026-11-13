import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import { SearchByEnum } from "@/shared/enums/backend/search-by-enum";
import type { ReviewedActionDto } from "@/shared/models/reviewed-action-dto";

/**
 * What a gesture becomes: a check, with its rule and what it tells Discord, or a step that is not
 * a click. The recording's window asks it and Review keeps it, with the same forms.
 */
export interface GestureFields {
  choice: ReviewChoiceEnum;

  searchBy: SearchByEnum;
  searchText: string;
  conditionType: ConditionTypeEnum;
  conditionTextEnd: string;

  notifyOnSuccess: boolean;
  notifyOnFailure: boolean;
  discordBotId: number | null;
  successMessage: string;
  failureMessage: string;

  commandText: string;
  inputName: string;
  message: string;
  isReadingText: boolean;
  readTextPattern: string;
  waitMilliseconds: number;
  stageName: string;
}

export interface RuleOption {
  label: string;
  value: ConditionTypeEnum;
}

export interface StepOption {
  label: string;
  value: ReviewChoiceEnum;
  description: string;
  icon: string;
}

export interface WaitUnit {
  label: string;
  value: number;
}

export const DEFAULT_WAIT_MILLISECONDS = 30_000;

// Read as the end of "The words read there ...".
export const CHECK_RULES: RuleOption[] = [
  { label: "contain", value: ConditionTypeEnum.CONTAINS },
  { label: "are exactly", value: ConditionTypeEnum.EQUALS },
  { label: "match a pattern", value: ConditionTypeEnum.MATCHES_REGEX },
  { label: "are a number above", value: ConditionTypeEnum.GREATER_THAN },
  { label: "are a number below", value: ConditionTypeEnum.LESS_THAN },
  { label: "are a number between", value: ConditionTypeEnum.BETWEEN },
  { label: "are not empty", value: ConditionTypeEnum.IS_NOT_EMPTY },
];

export const STEP_OPTIONS: StepOption[] = [
  {
    label: "Execute a command",
    value: ReviewChoiceEnum.EXECUTE_COMMAND,
    description: "A cmd command, such as clearing an export folder. Waits for it to finish.",
    icon: "code",
  },
  {
    label: "Open an application",
    value: ReviewChoiceEnum.LAUNCH_APP,
    description: "Starts a program and carries on without waiting for its window.",
    icon: "window-maximize",
  },
  {
    label: "Send a notification",
    value: ReviewChoiceEnum.NOTIFY,
    description: "Posts to a Discord channel through one of your bots, with the text read where you right clicked if you like.",
    icon: "bell",
  },
  {
    label: "Type an input's value",
    value: ReviewChoiceEnum.TYPE_INPUT,
    description: "Types an input the flow is given before each execution, such as an email.",
    icon: "pencil",
  },
  {
    label: "Wait a set time",
    value: ReviewChoiceEnum.WAIT,
    description: "For time the screen does not show, such as a game's timer.",
    icon: "clock",
  },
  {
    label: "Begin a stage",
    value: ReviewChoiceEnum.BEGIN_STAGE,
    description: "Names the part of the test that starts here.",
    icon: "flag",
  },
];

export const WAIT_UNITS: WaitUnit[] = [
  { label: "seconds", value: 1_000 },
  { label: "minutes", value: 60_000 },
  { label: "hours", value: 3_600_000 },
];

export function isCheckChoice(choice: ReviewChoiceEnum): boolean {
  return choice === ReviewChoiceEnum.CHECK_THERE || choice === ReviewChoiceEnum.CHECK_GONE;
}

export function isStepChoice(choice: ReviewChoiceEnum): boolean {
  return STEP_OPTIONS.some((x) => x.value === choice);
}

/** A notification that reads the words where it was marked first: the one step where the place matters. */
export function isReadingChoice(fields: GestureFields): boolean {
  return fields.choice === ReviewChoiceEnum.NOTIFY && fields.isReadingText;
}

/** Not empty, and empty, compare nothing. */
export function ruleNeedsWords(rule: ConditionTypeEnum): boolean {
  return rule !== ConditionTypeEnum.IS_EMPTY && rule !== ConditionTypeEnum.IS_NOT_EMPTY;
}

/** As the end of "Waits until the area ...". */
export function describeRule(rule: ConditionTypeEnum, text: string, end: string): string {
  const words = text.trim();

  switch (rule) {
    case ConditionTypeEnum.EQUALS:
      return `reads exactly “${words}”`;
    case ConditionTypeEnum.MATCHES_REGEX:
      return `reads something matching ${words}`;
    case ConditionTypeEnum.GREATER_THAN:
      return `reads a number above ${words}`;
    case ConditionTypeEnum.LESS_THAN:
      return `reads a number below ${words}`;
    case ConditionTypeEnum.BETWEEN:
      return `reads a number between ${words} and ${end.trim()}`;
    case ConditionTypeEnum.IS_NOT_EMPTY:
      return "reads anything at all";
    default:
      return `reads “${words}”`;
  }
}

/** A check's two notifications, as a check that waits for it to go or to be there says them. */
export function notifyLabels(choice: ReviewChoiceEnum): { success: string; failure: string } {
  if (choice === ReviewChoiceEnum.CHECK_GONE) return { success: "When it goes", failure: "When it stays" };

  return { success: "When it's there", failure: "When it never comes" };
}

/** What a step marked instead of a click does, said under its form. */
export function stepNote(fields: GestureFields): string {
  switch (fields.choice) {
    case ReviewChoiceEnum.EXECUTE_COMMAND:
      return "Executes the command in cmd and waits for it to finish. Windows only, as every command is for now.";
    case ReviewChoiceEnum.LAUNCH_APP:
      return "Opens the program and carries on without waiting for its window. To wait for it, Ctrl + click a check on its window next.";
    case ReviewChoiceEnum.TYPE_INPUT:
      return "Types the input's value. One the flow does not have yet is added with no value, to be given before it executes.";
    case ReviewChoiceEnum.NOTIFY:
      if (fields.isReadingText)
        return "Waits until the area reads anything, then posts the message with what it read, or why it read nothing, and the flow carries on either way. The flow's name is always sent with it.";
      return "Posts the message through the bot. The flow's name is always sent with it.";
    case ReviewChoiceEnum.WAIT:
      return "Every search already waits for what it looks for. This is for time the screen does not show, such as a game's timer running out.";
    case ReviewChoiceEnum.BEGIN_STAGE:
      return "It begins over the next click's search, as ## and its name: a label a report groups by.";
    default:
      return "";
  }
}

/** Letters, digits and underscores: what a script's input names are. */
export function toScriptName(text: string): string {
  return text.toLowerCase().replace(/[^a-z0-9_]/g, "_");
}

/** Whether what was answered can become its step: a bot to send through, something to execute. */
export function isComplete(fields: GestureFields): boolean {
  if (isCheckChoice(fields.choice)) {
    const isNotifying = fields.notifyOnSuccess || fields.notifyOnFailure;
    if (isNotifying && fields.discordBotId === null) return false;

    const isByWords = fields.searchBy === SearchByEnum.TEXT;
    if (isByWords && ruleNeedsWords(fields.conditionType) && fields.searchText.trim().length === 0) return false;
    if (isByWords && fields.conditionType === ConditionTypeEnum.BETWEEN && fields.conditionTextEnd.trim().length === 0) return false;

    return true;
  }

  switch (fields.choice) {
    case ReviewChoiceEnum.EXECUTE_COMMAND:
    case ReviewChoiceEnum.LAUNCH_APP:
      return fields.commandText.trim().length > 0;
    case ReviewChoiceEnum.TYPE_INPUT:
      return fields.inputName.trim().length > 0;
    case ReviewChoiceEnum.NOTIFY:
      return fields.discordBotId !== null;
    case ReviewChoiceEnum.WAIT:
      return fields.waitMilliseconds > 0;
    case ReviewChoiceEnum.BEGIN_STAGE:
      return fields.stageName.trim().length > 0;
    default:
      return true;
  }
}

/** Nothing decided: a check by its picture that waits for it, telling nobody. */
export function blankFields(choice: ReviewChoiceEnum): GestureFields {
  return {
    choice,
    searchBy: SearchByEnum.IMAGE,
    searchText: "",
    conditionType: ConditionTypeEnum.CONTAINS,
    conditionTextEnd: "",
    notifyOnSuccess: false,
    notifyOnFailure: false,
    discordBotId: null,
    successMessage: "",
    failureMessage: "",
    commandText: "",
    inputName: "",
    message: "",
    isReadingText: false,
    readTextPattern: "",
    waitMilliseconds: DEFAULT_WAIT_MILLISECONDS,
    stageName: "",
  };
}

/** What the fields say, as the backend keeps it: only what the choice uses. */
export function toReviewedFields(fields: GestureFields): Omit<ReviewedActionDto, "actionIndex" | "isSecret"> {
  const isCheck = isCheckChoice(fields.choice);
  const isByWords = isCheck && fields.searchBy === SearchByEnum.TEXT;
  const isNotifying = isCheck && (fields.notifyOnSuccess || fields.notifyOnFailure);
  const isNotify = fields.choice === ReviewChoiceEnum.NOTIFY;
  const isReading = isNotify && fields.isReadingText;
  const isCommand = fields.choice === ReviewChoiceEnum.EXECUTE_COMMAND || fields.choice === ReviewChoiceEnum.LAUNCH_APP;
  const stageName = fields.stageName.trim();
  const pattern = fields.readTextPattern.trim();

  return {
    choice: fields.choice,
    searchBy: fields.searchBy,
    searchText: fields.searchBy === SearchByEnum.TEXT ? fields.searchText : null,
    conditionType: isByWords ? fields.conditionType : null,
    conditionTextEnd: isByWords && fields.conditionType === ConditionTypeEnum.BETWEEN ? fields.conditionTextEnd : null,
    notifyOnSuccess: isCheck && fields.notifyOnSuccess,
    notifyOnFailure: isCheck && fields.notifyOnFailure,
    discordBotId: isNotifying || isNotify ? fields.discordBotId : null,
    successMessage: isCheck && fields.notifyOnSuccess ? fields.successMessage : null,
    failureMessage: isCheck && fields.notifyOnFailure ? fields.failureMessage : null,
    inputName: fields.choice === ReviewChoiceEnum.TYPE_INPUT ? fields.inputName : null,
    commandText: isCommand ? fields.commandText : null,
    message: isNotify ? fields.message : null,
    isReadingText: isReading,
    readTextPattern: isReading && pattern.length > 0 ? pattern : null,
    waitMilliseconds: fields.choice === ReviewChoiceEnum.WAIT ? fields.waitMilliseconds : 0,
    stageName: stageName.length > 0 ? stageName : null,
  };
}

/** The fields again, from what the backend kept, over what was there. */
export function fromReviewed(reviewed: ReviewedActionDto, over: GestureFields): GestureFields {
  return {
    ...over,
    choice: reviewed.choice,
    searchBy: reviewed.searchBy,
    searchText: reviewed.searchText ?? over.searchText,
    conditionType: reviewed.conditionType ?? over.conditionType,
    conditionTextEnd: reviewed.conditionTextEnd ?? over.conditionTextEnd,
    notifyOnSuccess: reviewed.notifyOnSuccess,
    notifyOnFailure: reviewed.notifyOnFailure,
    discordBotId: reviewed.discordBotId ?? over.discordBotId,
    successMessage: reviewed.successMessage ?? over.successMessage,
    failureMessage: reviewed.failureMessage ?? over.failureMessage,
    commandText: reviewed.commandText ?? over.commandText,
    inputName: reviewed.inputName ?? over.inputName,
    message: reviewed.message ?? over.message,
    isReadingText: reviewed.isReadingText,
    readTextPattern: reviewed.readTextPattern ?? over.readTextPattern,
    waitMilliseconds: reviewed.waitMilliseconds > 0 ? reviewed.waitMilliseconds : over.waitMilliseconds,
    stageName: reviewed.stageName ?? over.stageName,
  };
}
