/**
 * The whole main area at an action: the screenshot its press kept, or for a key or typed text,
 * the one from the last click before it.
 */
export interface RecordingScreenDto {
  /** The action whose press it is: the one asked about, or the last click before it. */
  actionIndex: number;

  /** The main area as a PNG, base64. */
  image: string;

  // Where the main area was on screen, so a click's place inside it is its location minus these.
  x: number;
  y: number;
  width: number;
  height: number;
  dpi: number;
}
