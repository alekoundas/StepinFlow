/** Hours when there are any, 1:54:46, otherwise minutes, 12:07. */
export const toElapsed = (milliseconds: number): string => {
  const seconds = Math.floor(Math.max(0, milliseconds) / 1000);
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const rest = String(seconds % 60).padStart(2, "0");

  if (hours > 0) return `${hours}:${String(minutes).padStart(2, "0")}:${rest}`;
  return `${minutes}:${rest}`;
};

/** How long something took, the way it is said: 2.4s, 13s, 27m 06s, 7h 49m. */
export const toTook = (milliseconds: number): string => {
  const exact = Math.max(0, milliseconds) / 1000;
  if (exact < 10) return `${exact.toFixed(1)}s`;

  const seconds = Math.round(exact);
  if (seconds < 60) return `${seconds}s`;

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  if (hours > 0) return `${hours}h ${String(minutes).padStart(2, "0")}m`;

  return `${minutes}m ${String(seconds % 60).padStart(2, "0")}s`;
};

/** Minutes and seconds, 01:12. */
export const toClock = (milliseconds: number): string => {
  const seconds = Math.floor(milliseconds / 1000);
  const minutes = Math.floor(seconds / 60);

  return `${String(minutes).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
};
