import { z } from "zod";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepGoBackSchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,
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
