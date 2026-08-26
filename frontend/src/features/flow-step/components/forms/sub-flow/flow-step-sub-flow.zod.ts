import { z } from "zod";

export const FlowStepSubFlowSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
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
