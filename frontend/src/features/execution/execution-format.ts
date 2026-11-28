import type { CSSProperties } from "react";

import { FlowStepCatalog } from "@/shared/models/flow-step-catalog";

const MONO = "ui-monospace, Consolas, monospace";

/** Found something. */
const FOUND_COLOR = "var(--primary-color)";

/** Failed, and its Failure branch took over: the flow working, not breaking. */
const HANDLED_COLOR = "var(--yellow-500)";

/** The one failure that ended the execution. */
const FATAL_COLOR = "var(--red-500)";

const DONE_COLOR = "var(--green-400)";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const CARD: CSSProperties = {
  background: "var(--surface-card)",
  borderColor: "var(--surface-border)",
};

const INSET: CSSProperties = {
  background: "var(--surface-ground)",
  borderColor: "var(--surface-border)",
};

/** A small outlined word beside a row's name. */
const chipStyle = (color: string): CSSProperties => ({
  height: 19,
  lineHeight: "17px",
  boxSizing: "border-box",
  padding: "0 7px",
  borderRadius: 6,
  border: `1px solid ${FlowStepCatalog.tint(color, 45)}`,
  color,
  fontSize: 11,
  whiteSpace: "nowrap",
  overflow: "hidden",
  textOverflow: "ellipsis",
});

export const ExecutionFormat = {
  MONO,
  FOUND_COLOR,
  HANDLED_COLOR,
  FATAL_COLOR,
  DONE_COLOR,
  MUTED,
  CARD,
  INSET,
  chipStyle,
};
