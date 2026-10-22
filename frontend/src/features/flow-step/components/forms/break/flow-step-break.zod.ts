import { z } from "zod";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepBreakSchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,
    flowStepReferenceId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowStepReferenceId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the step to break out of",
        path: ["flowStepReferenceId"],
      });
    }
  });
