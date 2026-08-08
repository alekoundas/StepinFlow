import { z } from "zod";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { cursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";

// The four cursor modes are separate FlowStepTypes so the tree, the icons and the executor keep a
// flat dispatch. Only the form merges them, and the mode buttons rewrite flowStepType.
export const CURSOR_FLOW_STEP_TYPES = [
  FlowStepTypeEnum.CURSOR_CLICK,
  FlowStepTypeEnum.CURSOR_RELOCATE,
  FlowStepTypeEnum.CURSOR_DRAG,
  FlowStepTypeEnum.CURSOR_SCROLL,
] as const;

export type CursorFlowStepType = (typeof CURSOR_FLOW_STEP_TYPES)[number];

export const isCursorFlowStepType = (
  type: FlowStepTypeEnum | undefined,
): type is CursorFlowStepType =>
  CURSOR_FLOW_STEP_TYPES.includes(type as CursorFlowStepType);

export const FlowStepCursorSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    flowStepType: z.enum(CURSOR_FLOW_STEP_TYPES),

    // Start point
    isLocationCustom: z.boolean(),
    flowLocationId: z.number().int().nullish(),
    flowStepReferenceId: z.number().int().nullish(),

    // End point (CURSOR_DRAG)
    isLocationEndCustom: z.boolean(),
    flowLocationEndId: z.number().int().nullish(),
    flowStepReferenceEndId: z.number().int().nullish(),

    // CURSOR_CLICK / CURSOR_DRAG
    cursorButtonActionType: z.enum(cursorButtonActionTypeEnum).nullish(),
    cursorButtonType: z.enum(CursorButtonTypeEnum).nullish(),

    // CURSOR_SCROLL
    cursorScrollDirectionType: z.enum(CursorScrollDirectionTypeEnum).nullish(),
    loopCount: z.number().int().min(0).max(2147483647),
  })
  .superRefine((data, ctx) => {
    const needsStartPoint =
      data.flowStepType === FlowStepTypeEnum.CURSOR_CLICK ||
      data.flowStepType === FlowStepTypeEnum.CURSOR_RELOCATE ||
      data.flowStepType === FlowStepTypeEnum.CURSOR_DRAG;

    if (needsStartPoint) {
      if (data.isLocationCustom && !data.flowLocationId) {
        ctx.addIssue({
          code: "custom",
          message: "Pick a location",
          path: ["flowLocationId"],
        });
      }
      if (!data.isLocationCustom && !data.flowStepReferenceId) {
        ctx.addIssue({
          code: "custom",
          message: "Pick the step whose result to use",
          path: ["flowStepReferenceId"],
        });
      }
    }

    if (data.flowStepType === FlowStepTypeEnum.CURSOR_DRAG) {
      if (data.isLocationEndCustom && !data.flowLocationEndId) {
        ctx.addIssue({
          code: "custom",
          message: "Pick a drop location",
          path: ["flowLocationEndId"],
        });
      }
      if (!data.isLocationEndCustom && !data.flowStepReferenceEndId) {
        ctx.addIssue({
          code: "custom",
          message: "Pick the step whose result to use",
          path: ["flowStepReferenceEndId"],
        });
      }
    }

    if (data.flowStepType === FlowStepTypeEnum.CURSOR_CLICK) {
      if (!data.cursorButtonActionType) {
        ctx.addIssue({
          code: "custom",
          message: "Please select a click action",
          path: ["cursorActionType"],
        });
      }
    }

    if (
      data.flowStepType === FlowStepTypeEnum.CURSOR_CLICK ||
      data.flowStepType === FlowStepTypeEnum.CURSOR_DRAG
    ) {
      if (!data.cursorButtonType) {
        ctx.addIssue({
          code: "custom",
          message: "Please select a mouse button",
          path: ["cursorButtonType"],
        });
      }
    }

    if (data.flowStepType === FlowStepTypeEnum.CURSOR_SCROLL) {
      if (!data.cursorScrollDirectionType) {
        ctx.addIssue({
          code: "custom",
          message: "Please select a scroll direction",
          path: ["cursorScrollDirectionType"],
        });
      }
      if (data.loopCount < 1) {
        ctx.addIssue({
          code: "custom",
          message: "Scroll amount must be at least 1",
          path: ["loopCount"],
        });
      }
    }
  });
