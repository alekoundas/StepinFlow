import type { CSSProperties } from "react";

import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import { daysAgo, shortDate } from "@/shared/utils/day-and-time";

export const SURFACE: CSSProperties = {
  background: "color-mix(in srgb, var(--surface-card) 55%, var(--surface-ground))",
  borderColor: "var(--surface-border)",
};

const STOPPED_COLOR = "var(--bluegray-400)";

/** How an outcome looks wherever the executions page shows one: its colour, its icon and its words. */
export interface OutcomeLook {
  color: string;
  icon: string;
  text: string;

  /** Stopped or abandoned: it ended without saying how it went. */
  isWithoutVerdict: boolean;
}

export const outcomeLookOf = (status: ExecutionStatusEnum, errorStepName?: string | null): OutcomeLook => {
  switch (status) {
    case ExecutionStatusEnum.RUNNING:
      return { color: "var(--blue-400)", icon: "pi pi-spin pi-spinner", text: "Executing", isWithoutVerdict: false };
    case ExecutionStatusEnum.COMPLETED:
      return { color: "var(--green-400)", icon: "pi pi-check", text: "Completed", isWithoutVerdict: false };
    case ExecutionStatusEnum.FAILED:
      return { color: "var(--red-400)", icon: "pi pi-times", text: "Failed", isWithoutVerdict: false };
    case ExecutionStatusEnum.ERRORED:
      return {
        color: "var(--red-400)",
        icon: "pi pi-exclamation-triangle",
        text: errorStepName ? `Errored at ${errorStepName}` : "Errored",
        isWithoutVerdict: false,
      };
    case ExecutionStatusEnum.INCONCLUSIVE:
      return { color: "var(--orange-400)", icon: "pi pi-question", text: "Inconclusive", isWithoutVerdict: false };
    case ExecutionStatusEnum.STOPPED:
      return { color: STOPPED_COLOR, icon: "pi pi-stop", text: "Stopped by you", isWithoutVerdict: true };
    default:
      return { color: STOPPED_COLOR, icon: "pi pi-power-off", text: "Abandoned when the app closed", isWithoutVerdict: true };
  }
};

/** Completed out of those that ended with a verdict. Null when none did, which is not 0%. */
export const completedPercentOf = (completed: number, failed: number): number | null => {
  if (completed + failed === 0) return null;

  return Math.round((completed / (completed + failed)) * 100);
};

/** "Today", "Yesterday", "5 Nov". */
export const dayOf = (value: string): string => {
  const date = new Date(value);
  const days = daysAgo(date);

  if (days === 0) return "Today";
  if (days === 1) return "Yesterday";

  return shortDate(date);
};

/** "Today · 7 Nov", "Yesterday · 6 Nov", "5 Nov", and the year once it is not this one. */
export const dayLabelOf = (value: string): string => {
  const date = new Date(value);
  const days = daysAgo(date);

  if (days === 0) return `Today · ${shortDate(date)}`;
  if (days === 1) return `Yesterday · ${shortDate(date)}`;
  if (date.getFullYear() !== new Date().getFullYear()) return `${shortDate(date)} ${date.getFullYear()}`;

  return shortDate(date);
};

/** "1 step", "137,040 steps". */
export const stepsText = (count: number): string => `${count.toLocaleString()} ${count === 1 ? "step" : "steps"}`;

export const timeOf = (value: string): string =>
  new Date(value).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
