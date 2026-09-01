/**
 * A local model worth offering to somebody who has none. Ollama's library is far larger; a short
 * list is there so a first choice is obvious rather than researched.
 */
export interface AiModelSuggestionDto {
  name: string;

  /** Roughly what will be downloaded. Approximate, and only there to set expectations. */
  size: string;
  description: string;

  /** Already pulled, so the row says so rather than offering it again. */
  isInstalled: boolean;
}
