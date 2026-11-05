import type { CSSProperties } from "react";

export const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

/** The rail's colour, so the cards and the rail read as one surface. */
export const CARD_BACKGROUND = "color-mix(in srgb, var(--surface-card) 55%, var(--surface-ground))";

/** A tinted square or circle behind an icon. */
export const iconTile = (color: string): CSSProperties => ({
  background: `color-mix(in srgb, ${color} 14%, transparent)`,
  color,
});
