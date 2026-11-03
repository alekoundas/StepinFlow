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
