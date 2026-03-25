// schemas/base-flow-step.schema.ts
import { z } from "zod";

export const FlowStepWaitSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  waitForMilliseconds: z.number().int().min(50, "Minimum 50 ms").max(300000),
});
// .refine((data) => data.flowStepType === FlowStepTypeEnum.WAIT, {
//   message: "flowStepType must be WAIT for this form",
//   path: ["flowStepType"],
// });
