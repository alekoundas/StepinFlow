import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { WindowFlowStepType } from "@/features/flow-step/components/forms/window/flow-step-window.zod";

export interface WindowMode {
  flowStepType: WindowFlowStepType;
  label: string;
  iconName: string;
  description: string;
}

// Three FlowStepTypes, one form. Same shape as the cursor modes.
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
];
