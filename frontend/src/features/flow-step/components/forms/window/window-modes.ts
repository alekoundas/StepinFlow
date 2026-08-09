import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { WindowFlowStepType } from "@/features/flow-step/components/forms/window/flow-step-window.zod";

export interface WindowMode {
  flowStepType: WindowFlowStepType;
  label: string;
  iconName: string;
  defaultName: string;
  description: string;
}

// Three FlowStepTypes, one form. Same shape as the cursor modes.
export const WINDOW_MODES: WindowMode[] = [
  {
    flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
    label: "Focus",
    iconName: "window-maximize",
    defaultName: "Window Focus",
    description: "Bring the window to the foreground before anything else runs.",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RESIZE,
    label: "Resize",
    iconName: "expand",
    defaultName: "Window Resize",
    description:
      "Force the window to a fixed size. Every area and location inside it then means the same thing on any machine.",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RELOCATE,
    label: "Move",
    iconName: "directions",
    defaultName: "Window Move",
    description: "Move the window to a saved location.",
  },
];

export const WINDOW_STEP_DEFAULT_NAMES = Object.fromEntries(
  WINDOW_MODES.map((x) => [x.flowStepType, x.defaultName]),
) as Record<WindowFlowStepType, string>;
