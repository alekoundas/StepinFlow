import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { CursorFlowStepType } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";

export interface CursorMode {
  flowStepType: CursorFlowStepType;
  label: string;
  iconName: string;
  defaultName: string;
  description: string;
}

// The four cursor modes are separate FlowStepTypes so the tree, the icons and the executor keep a
// flat dispatch. Only the form merges them, and the mode buttons rewrite flowStepType.
export const CURSOR_MODES: CursorMode[] = [
  {
    flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
    label: "Click",
    iconName: "mouse",
    defaultName: "Cursor Click",
    description:
      "Press a mouse button at the resolved location. Single, double, hold or release.",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
    label: "Move",
    iconName: "map-marker",
    defaultName: "Cursor Move",
    description: "Move the cursor to the resolved location without clicking.",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
    label: "Drag",
    iconName: "arrows-alt",
    defaultName: "Cursor Drag & Drop",
    description:
      "Hold at the grab point, drag to the drop point, then release.",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
    label: "Scroll",
    iconName: "sort-alt",
    defaultName: "Cursor Scroll",
    description: "Turn the mouse wheel a number of notches in one direction.",
  },
];

export const CURSOR_STEP_DEFAULT_NAMES = Object.fromEntries(
  CURSOR_MODES.map((x) => [x.flowStepType, x.defaultName]),
) as Record<CursorFlowStepType, string>;
