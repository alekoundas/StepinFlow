import type { CSSProperties } from "react";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

/** The rail's colour, so the cards and the rail read as one surface. */
const CARD_BACKGROUND = "color-mix(in srgb, var(--surface-card) 55%, var(--surface-ground))";

/** "340 MB", "1.2 GB". */
const diskSize = (bytes: number): string => {
  const megabytes = bytes / 1024 / 1024;
  if (megabytes < 1024) return `${Math.round(megabytes)} MB`;

  return `${(megabytes / 1024).toFixed(1)} GB`;
};

/** A tinted square or circle behind an icon. */
const iconTile = (color: string): CSSProperties => ({
  background: `color-mix(in srgb, ${color} 14%, transparent)`,
  color,
});

export const HomeFormat = {
  MUTED,
  CARD_BACKGROUND,
  diskSize,
  iconTile,
};
