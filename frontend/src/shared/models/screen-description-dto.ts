/**
 * What the local model saw at one press, asked as a reviewed recording goes to a model: a sentence
 * for the page where one began, a few words for a click nothing else named.
 */
export interface ScreenDescriptionDto {
  /** The press's action, by its index among the recording's actions. */
  actionIndex: number;

  /** Which page, screen or dialog it is, in a sentence. */
  page?: string | null;

  /** What was pressed, in a few words, when neither OCR nor Windows named it. */
  element?: string | null;
}
