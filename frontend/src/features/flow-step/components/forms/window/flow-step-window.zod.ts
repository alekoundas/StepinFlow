import { z } from "zod";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export const WINDOW_FLOW_STEP_TYPES = [
  FlowStepTypeEnum.WINDOW_FOCUS,
  FlowStepTypeEnum.WINDOW_RESIZE,
  FlowStepTypeEnum.WINDOW_RELOCATE,
] as const;

export type WindowFlowStepType = (typeof WINDOW_FLOW_STEP_TYPES)[number];

export const isWindowFlowStepType = (
  type: FlowStepTypeEnum | undefined,
): type is WindowFlowStepType =>
  WINDOW_FLOW_STEP_TYPES.includes(type as WindowFlowStepType);

export const FlowStepWindowSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    flowStepType: z.enum(WINDOW_FLOW_STEP_TYPES),

    // The window itself, an APPLICATION area.
    flowAreaId: z.number().int().nullish(),

    // WINDOW_RESIZE
    windowWidth: z.number().int().min(0).max(2147483647),
    windowHeight: z.number().int().min(0).max(2147483647),

    // WINDOW_RELOCATE
    flowPointId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowAreaId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the window this step acts on",
        path: ["flowAreaId"],
      });
    }

    if (data.flowStepType === FlowStepTypeEnum.WINDOW_RESIZE) {
      if (data.windowWidth < 1 || data.windowHeight < 1) {
        ctx.addIssue({
          code: "custom",
          message: "Width and height are required",
          path: ["windowWidth"],
        });
      }
    }

    if (data.flowStepType === FlowStepTypeEnum.WINDOW_RELOCATE) {
      if (!data.flowPointId) {
        ctx.addIssue({
          code: "custom",
          message: "Pick where to move the window",
          path: ["flowPointId"],
        });
      }
    }
  });
