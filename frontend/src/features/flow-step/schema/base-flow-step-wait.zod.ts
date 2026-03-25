// schemas/base-flow-step.schema.ts
import { z } from "zod";

export const FlowStepWaitSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  waitForMilliseconds: z
    .number()
    .int()
    .min(50, "Minimum 50 ms")
    .max(2147483647, "Maximum is 2.147.483.647"), // signed int32 Max
});
// .refine((data) => data.flowStepType === FlowStepTypeEnum.WAIT, {
//   message: "flowStepType must be WAIT for this form",
//   path: ["flowStepType"],
// });
