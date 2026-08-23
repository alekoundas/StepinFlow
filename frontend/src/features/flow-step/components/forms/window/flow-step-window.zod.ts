import { z } from "zod";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

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

    // The window itself. Named here rather than borrowed from an APPLICATION area: a matcher is
    // the same on every machine, so it has no reason to be a separate tunable record.
    processName: z.string(),
    titlePattern: z.string(),
    titleMatchMode: z.enum(TitleMatchModeEnum),

    // WINDOW_RESIZE
    windowWidth: z.number().int().min(0).max(2147483647),
    windowHeight: z.number().int().min(0).max(2147483647),

    // WINDOW_RELOCATE
    flowPointId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    // Either half is enough. Matching on nothing would act on whatever window is in front.
    if (data.processName.length === 0 && data.titlePattern.length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Pick an application, or type a title to match",
        path: ["titlePattern"],
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
