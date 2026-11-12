/**
 * Which page of the main area a press was on: pages are numbered from 1 in the order first seen,
 * and the same number again is a page come back to.
 */
export interface ScreenPageDto {
  /** The press's action, by its index among the recording's actions. */
  actionIndex: number;

  page: number;
}
