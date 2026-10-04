import { z } from "zod";
import { scriptName } from "@/shared/utils/script-text";

export const FlowStepSubFlowSchema = z
  .object({
    name: scriptName(),
    subFlowId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    if (!data.subFlowId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the flow to run",
        path: ["subFlowId"],
      });
    }
  });
