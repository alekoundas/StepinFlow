import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import {
  cursorButtonActionTypeEnum,
  type CursorButtonActionTypeEnum,
} from "@/shared/enums/backend/cursor-button-action-type-enum";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorFlowStepType } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";

/** What a step of a mode starts with, and what switching to it puts in place of the last mode's. */
export interface CursorModeDefaults {
  cursorButtonActionType: CursorButtonActionTypeEnum | undefined;
  cursorButtonType: CursorButtonTypeEnum | undefined;
}

export interface CursorMode {
  flowStepType: CursorFlowStepType;
  label: string;
  iconName: string;
  description: string;
  defaults: CursorModeDefaults;
}

const NO_BUTTON: CursorModeDefaults = { cursorButtonActionType: undefined, cursorButtonType: undefined };

// The four cursor modes are separate FlowStepTypes so the tree, the icons and the executor keep a
// flat dispatch. Only the form merges them, and the mode buttons rewrite flowStepType.
export const CURSOR_MODES: CursorMode[] = [
  {
    flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
    label: "Click",
    iconName: "bullseye",
    description:
      "Press a mouse button where the cursor is. Single, double, hold or release. A Move above it says where.",
    defaults: {
      cursorButtonActionType: cursorButtonActionTypeEnum.SINGLE_CLICK,
      cursorButtonType: CursorButtonTypeEnum.LEFT_BUTTON,
    },
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
    label: "Move",
    iconName: "map-marker",
    description: "Move the cursor to the resolved location without clicking.",
    defaults: NO_BUTTON,
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
    label: "Drag",
    iconName: "arrows-alt",
    description:
      "Hold at the grab point, drag to the drop point, then release.",
    defaults: { cursorButtonActionType: undefined, cursorButtonType: CursorButtonTypeEnum.LEFT_BUTTON },
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
    label: "Scroll",
    iconName: "sort-alt",
    description: "Turn the mouse wheel where the cursor is, a number of notches in one direction.",
    defaults: NO_BUTTON,
  },
];

export const CURSOR_STEP_DEFAULTS = Object.fromEntries(
  CURSOR_MODES.map((x) => [x.flowStepType, x.defaults]),
) as Record<CursorFlowStepType, CursorModeDefaults>;
