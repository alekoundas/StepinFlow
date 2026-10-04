import { z } from "zod";
import { scriptName } from "@/shared/utils/script-text";

export const FlowStepLoopSchema = z
  .object({
    name: scriptName(),
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
