import { z } from "zod";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepLoopSchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,
    name: z
      .string()
      .min(1, "Name is required")
      .max(120, "Name too long")
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
    loopCount: z
      .number()
      .int()
      .min(0, "Loop count must be 0 or greater")
      .max(2147483647),
    isLoopInfinite: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (data.isLoopInfinite === false && data.loopCount <= 0) {
      const message =
        "Either enable Infinite Loop or set Loop Count greater than 0";

      ctx.addIssue({
        code: "custom",
        message,
        path: ["loopCount"],
      });

      ctx.addIssue({
        code: "custom",
        message,
        path: ["isLoopInfinite"],
      });
    }
  });
