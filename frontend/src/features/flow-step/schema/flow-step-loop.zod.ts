import { z } from "zod";

export const FlowStepLoopSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    loopCount: z
      .number()
      .int()
      .min(0, "Loop count must be 0 or greater")
      .max(2147483647),
    isLoopInfinite: z.boolean(),
  })
  .superRefine((data, ctx) => {
    const hasInfinite = data.isLoopInfinite === true;
    const hasFiniteLoop = data.loopCount > 0;

    if (!hasInfinite && !hasFiniteLoop) {
      ctx.addIssue({
        code: "custom",
        message: "Either enable Infinite Loop or set Loop Count greater than 0",
        path: ["isLoopInfinite"], // error appears near the checkbox
      });
      ctx.addIssue({
        code: "custom",
        message: "Either enable Infinite Loop or set Loop Count greater than 0",
        path: ["loopCount"],
      });
    }
  });
