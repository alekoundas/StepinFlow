import type { ComponentType } from "react";

import type { FormMode } from "@/shared/enums/form-mode-enum";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import FlowStepWaitFormComponent from "@/features/flow-step/components/forms/wait/FlowStepWaitFormComponent";
import FlowStepLoopFormComponent from "@/features/flow-step/components/forms/loop/FlowStepLoopFormComponent";
import FlowStepCursorFormComponent from "@/features/flow-step/components/forms/cursor/FlowStepCursorFormComponent";
import FlowStepWindowFormComponent from "@/features/flow-step/components/forms/window/FlowStepWindowFormComponent";
import FlowStepImageSearchFormComponent from "@/features/flow-step/components/forms/image-search/FlowStepImageSearchFormComponent";
import FlowStepReadTextFormComponent from "@/features/flow-step/components/forms/read-text/FlowStepReadTextFormComponent";
import FlowStepCheckValueFormComponent from "@/features/flow-step/components/forms/check-value/FlowStepCheckValueFormComponent";
import FlowStepSystemCommandFormComponent from "@/features/flow-step/components/forms/system-command/FlowStepSystemCommandFormComponent";
import FlowStepSystemActionFormComponent from "@/features/flow-step/components/forms/system-action/FlowStepSystemActionFormComponent";
import FlowStepSubFlowFormComponent from "@/features/flow-step/components/forms/sub-flow/FlowStepSubFlowFormComponent";

import {
  CURSOR_FLOW_STEP_TYPES,
  type CursorFlowStepType,
} from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";
import { CURSOR_STEP_DEFAULT_NAMES } from "@/features/flow-step/components/forms/cursor/cursor-modes";
import {
  WINDOW_FLOW_STEP_TYPES,
  type WindowFlowStepType,
} from "@/features/flow-step/components/forms/window/flow-step-window.zod";
import { WINDOW_STEP_DEFAULT_NAMES } from "@/features/flow-step/components/forms/window/window-modes";
import { SYSTEM_ACTIONS } from "@/features/flow-step/components/forms/system-action/system-actions";

/** Every step form takes the same props, which is what lets one caller render all of them. */
export interface FlowStepFormProps {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export interface FlowStepFormEntry {
  component: ComponentType<FlowStepFormProps>;

  /**
   * What a brand new step of this type starts as, beyond the fields every step shares. Takes the
   * type because the cursor and window forms each cover several of them.
   */
  newStepValues: (flowStepType: FlowStepTypeEnum) => Partial<FlowStepDto>;
}

const forTypes = (
  types: readonly FlowStepTypeEnum[],
  entry: FlowStepFormEntry,
): Partial<Record<FlowStepTypeEnum, FlowStepFormEntry>> =>
  Object.fromEntries(types.map((type) => [type, entry]));

/**
 * The one place a step type is tied to the form that edits it. Adding a type means one entry
 * here, not a branch in the add path and another in the edit path.
 *
 * Types with no entry have no form yet, which is a real state: they exist in the enum and the
 * tree renders them, but they cannot be authored.
 */
const FLOW_STEP_FORMS: Partial<Record<FlowStepTypeEnum, FlowStepFormEntry>> = {
  // All four cursor types share one form, the mode buttons switch flowStepType.
  ...forTypes(CURSOR_FLOW_STEP_TYPES, {
    component: FlowStepCursorFormComponent,
    newStepValues: (type) => ({
      name: CURSOR_STEP_DEFAULT_NAMES[type as CursorFlowStepType],
      isPointCustom: true,
      isPointEndCustom: true,
    }),
  }),

  // The three window types share one form too.
  ...forTypes(WINDOW_FLOW_STEP_TYPES, {
    component: FlowStepWindowFormComponent,
    newStepValues: (type) => ({
      name: WINDOW_STEP_DEFAULT_NAMES[type as WindowFlowStepType],
    }),
  }),

  [FlowStepTypeEnum.WAIT]: {
    component: FlowStepWaitFormComponent,
    newStepValues: () => ({ name: "Wait", waitForMilliseconds: 50 }),
  },

  [FlowStepTypeEnum.LOOP]: {
    component: FlowStepLoopFormComponent,
    newStepValues: () => ({ name: "Loop" }),
  },

  [FlowStepTypeEnum.IMAGE_SEARCH]: {
    component: FlowStepImageSearchFormComponent,
    newStepValues: () => ({ name: "Image Search" }),
  },

  [FlowStepTypeEnum.READ_TEXT]: {
    component: FlowStepReadTextFormComponent,
    newStepValues: () => ({
      name: "Read Text",
      conditionType: ConditionTypeEnum.CONTAINS,
    }),
  },

  [FlowStepTypeEnum.CHECK_VALUE]: {
    component: FlowStepCheckValueFormComponent,
    newStepValues: () => ({
      name: "Check Value",
      conditionType: ConditionTypeEnum.CONTAINS,
    }),
  },

  [FlowStepTypeEnum.SYSTEM_COMMAND]: {
    component: FlowStepSystemCommandFormComponent,
    newStepValues: () => ({ name: "System Command" }),
  },

  [FlowStepTypeEnum.SUB_FLOW]: {
    component: FlowStepSubFlowFormComponent,
    newStepValues: () => ({ name: "Sub-Flow" }),
  },

  [FlowStepTypeEnum.SYSTEM_ACTION]: {
    component: FlowStepSystemActionFormComponent,
    newStepValues: () => ({ name: SYSTEM_ACTIONS[0].defaultName }),
  },
};

export const getFlowStepForm = (
  flowStepType: FlowStepTypeEnum | undefined,
): FlowStepFormEntry | undefined =>
  flowStepType ? FLOW_STEP_FORMS[flowStepType] : undefined;
