import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { WindowFlowStepType } from "@/features/flow-step/components/forms/window/flow-step-window.zod";

export interface WindowMode {
  flowStepType: WindowFlowStepType;
  label: string;
  iconName: string;
  description: string;
}

// Four FlowStepTypes, one form. Same shape as the cursor modes.
export const WINDOW_MODES: WindowMode[] = [
  {
    flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
    label: "Focus",
    iconName: "window-maximize",
    description: "Bring the window to the foreground before anything else runs.",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RESIZE,
    label: "Resize",
    iconName: "expand",
    description:
      "Force the window to a fixed size. Every area and location inside it then means the same thing on any machine.",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RELOCATE,
    label: "Move",
    iconName: "directions",
    description: "Move the window to a saved location.",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_CLOSE,
    label: "Close",
    iconName: "window-minimize",
    description:
      "Close the window the way its X does, and wait for it to go. For a browser tab, only that tab closes.",
  },
];

/** What a window step is called until someone types their own. Mirrors FlowStepNameHelper.NameOf. */
export const windowStepName = (
  flowStepType: WindowFlowStepType,
  windowWidth: number | null | undefined,
  windowHeight: number | null | undefined,
  isCloseForced: boolean | undefined,
): string => {
  switch (flowStepType) {
    case FlowStepTypeEnum.WINDOW_FOCUS:
      return "Focus window";
    case FlowStepTypeEnum.WINDOW_RESIZE:
      return `Resize window to ${windowWidth ?? 0}x${windowHeight ?? 0}`;
    case FlowStepTypeEnum.WINDOW_RELOCATE:
      return "Move window";
    case FlowStepTypeEnum.WINDOW_CLOSE:
      return isCloseForced ? "Close window, forced" : "Close window";
  }
};

/** Nothing typed: empty, that name, or that name with the number a clash gave it. */
export const followsWindowStepName = (name: string, settingsName: string): boolean => {
  const typed = name.trim();
  if (typed.length === 0 || typed === settingsName) return true;
  if (!typed.startsWith(`${settingsName} `)) return false;

  return /^(\d+|\(\d+\))$/.test(typed.slice(settingsName.length + 1));
};
