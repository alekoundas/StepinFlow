import type { CSSProperties } from "react";

import { tint } from "@/shared/models/flow-step-catalog";

export const MONO = "ui-monospace, Consolas, monospace";

/** Found something. */
export const FOUND_COLOR = "var(--primary-color)";

/** Failed, and its Failure branch took over: the flow working, not breaking. */
export const HANDLED_COLOR = "var(--yellow-500)";

/** The one failure that ended the execution. */
export const FATAL_COLOR = "var(--red-500)";

export const DONE_COLOR = "var(--green-400)";

export const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

export const CARD: CSSProperties = {
  background: "var(--surface-card)",
  borderColor: "var(--surface-border)",
};

export const INSET: CSSProperties = {
  background: "var(--surface-ground)",
  borderColor: "var(--surface-border)",
};

/** A small outlined word beside a row's name. */
export const chipStyle = (color: string): CSSProperties => ({
  height: 19,
  lineHeight: "17px",
  boxSizing: "border-box",
  padding: "0 7px",
  borderRadius: 6,
  border: `1px solid ${tint(color, 45)}`,
  color,
  fontSize: 11,
  whiteSpace: "nowrap",
  overflow: "hidden",
  textOverflow: "ellipsis",
});
