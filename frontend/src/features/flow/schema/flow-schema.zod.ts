// schemas/base-flow-step.schema.ts
import { z } from "zod";

export const FlowSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  orderNumber: z
    .number()
    .int()
    .min(0, "Order must be >= 0")
    .max(2147483647, "Order too large"),
});
// .refine((data) => data.flowStepType === FlowStepTypeEnum.WAIT, {
//   message: "flowStepType must be WAIT for this form",
//   path: ["flowStepType"],
// });
