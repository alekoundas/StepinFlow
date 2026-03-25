// schemas/base-flow-step.schema.ts
import { z } from "zod";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { CursorActionTypeEnum } from "@/shared/enums/backend/cursor-action-type-enum";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";

export const BaseFlowStepSchema = z.object({
  // Core fields
  id: z.number().int().min(-1, "ID must be -1 for new steps or >= 0"),
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  flowStepType: z.enum(Object.values(FlowStepTypeEnum)),
  orderNumber: z.number().int().min(0, "Order number is less than 0"),

  // Location
  locationX: z.number().int(),
  locationY: z.number().int(),
  locationEndX: z.number().int(),
  locationEndY: z.number().int(),

  // WAIT
  waitForMilliseconds: z.number().int().min(0),

  // LOOP
  loopCount: z.number().int().min(0),
  isLoopInfinite: z.boolean(),

  // RUN_CMD
  runCommand: z.string(),

  // VARIABLE_CONDITION
  conditionText: z.string(),
  conditionType: z.enum(Object.values(ConditionTypeEnum)),

  // WINDOW_*
  windowName: z.string(),
  windowHeight: z.number().int().min(0),
  windowWidth: z.number().int().min(0),

  // KEYBOARD_INPUT
  keyboardInputText: z.string(),
  keyboardInputType: z.enum(Object.values(KeyboardInputTypeEnum)).optional(),

  // CURSOR_*
  isLocationCustom: z.boolean(),
  isLocationEndCustom: z.boolean(),
  cursorActionType: z.enum(Object.values(CursorActionTypeEnum)).optional(),
  cursorButtonType: z.enum(Object.values(CursorButtonTypeEnum)).optional(),
  cursorScrollDirectionType: z
    .enum(Object.values(CursorScrollDirectionTypeEnum))
    .optional(),

  // Relations
  rootId: z.number().int().min(-1),
  flowId: z.number().int().optional(),
  subFlowId: z.number().int().optional(),
  flowSearchAreaId: z.number().int().optional(),
  parentFlowStepId: z.number().int().optional(),
  flowStepReferenceId: z.number().int().optional(),

  // Collections
  childrenFlowSteps: z.array(z.any()),
  flowStepReferences: z.array(z.any()),
  flowStepImages: z.array(z.any()),
});
