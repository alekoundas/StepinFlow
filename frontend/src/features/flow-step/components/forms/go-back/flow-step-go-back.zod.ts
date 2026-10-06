import { z } from "zod";

export const FlowStepGoBackSchema = z
  .object({
    name: z
      .string()
      .min(1, "Name is required")
      .max(120, "Name too long")
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
    flowStepReferenceId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowStepReferenceId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the step to go back to",
        path: ["flowStepReferenceId"],
      });
    }
  });
