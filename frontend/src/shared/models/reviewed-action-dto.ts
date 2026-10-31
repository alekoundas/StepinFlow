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

  /** For a search by its words: the words it waits to read. */
  searchText?: string | null;

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
