import { z } from "zod";

export const FlowStepWaitSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    waitForMilliseconds: z
      .number()
      .int()
      .min(50, "Minimum 50 ms")
      .max(2147483647, "Maximum is 2.147.483.647"), // signed int32 Max

    /** Upper bound of the random range. 0 means wait exactly waitForMilliseconds. */
    waitForMillisecondsMax: z
      .number()
      .int()
      .min(0)
      .max(2147483647, "Maximum is 2.147.483.647"),
  })
  .superRefine((data, ctx) => {
    // Zero is "not a range" rather than a bad one, so only a set maximum is checked.
    if (data.waitForMillisecondsMax > 0 && data.waitForMillisecondsMax <= data.waitForMilliseconds) {
      ctx.addIssue({
        code: "custom",
        message: "The longest wait has to be longer than the shortest",
        path: ["waitForMillisecondsMax"],
      });
    }
  });
