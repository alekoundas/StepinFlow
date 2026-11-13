import type { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import type { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";
import type { SearchByEnum } from "@/shared/enums/backend/search-by-enum";

/** What the tester decided about one recorded action, in Review. */
export interface ReviewedActionDto {
  actionIndex: number;
  choice: ReviewChoiceEnum;

  // The element's rectangle as reviewed, in the main area's pixels. Null for an action with none.
  // Searched by its picture, the template is cut along it; by its words, they are read inside it.
  elementX?: number | null;
  elementY?: number | null;
  elementWidth?: number | null;
  elementHeight?: number | null;

  searchBy: SearchByEnum;

  /** For a search by its words: the words it waits to read, or the first number its rule compares with. */
  searchText?: string | null;

  /** For a check by its words: how the words read are compared. Null is contains, as a click's search does. */
  conditionType?: ConditionTypeEnum | null;

  /** For a check by its words between two numbers: the upper one. */
  conditionTextEnd?: string | null;

  /** For a check: a Notify first in its Success branch, with the check's result. */
  notifyOnSuccess: boolean;

  /** For a check: a Notify before the execution ends in its Failure branch, with why it failed. */
  notifyOnFailure: boolean;

  /** For a check's notifications, or a notification marked instead of a click: the bot it sends through. */
  discordBotId?: number | null;

  successMessage?: string | null;
  failureMessage?: string | null;

  /** For a notification marked instead of a click: what it says. */
  message?: string | null;

  /** For a notification marked instead of a click: the words inside the rectangle are read first, and sent with it. */
  isReadingText: boolean;

  /** For a notification that reads the words: a pattern for the part of them it keeps. Empty keeps them all. */
  readTextPattern?: string | null;

  /** For a wait marked instead of a click: how long. */
  waitMilliseconds: number;

  /** For typed text that becomes an input, or an input's value typed: its name. */
  inputName?: string | null;

  /** For an input: never written to a file, never shown to a model. */
  isSecret: boolean;

  /** For a command: what it executes. For an application: the program that opens it. */
  commandText?: string | null;

  /**
   * For a stage marked while recording, its name. For any other action, a stage the tester named as
   * beginning there - where a new page began. Empty is no stage.
   */
  stageName?: string | null;
}
