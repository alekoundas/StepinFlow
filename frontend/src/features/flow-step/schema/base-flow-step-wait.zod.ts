// schemas/base-flow-step.schema.ts
import { z } from "zod";
import { BaseFlowStepSchema } from "@/features/flow-step/schema/base-flow-step.zod";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export const FlowStepWaitSchema = BaseFlowStepSchema.extend({
  // flowStepType: z.literal(FlowStepTypeEnum.WAIT),
  waitForMilliseconds: z.number().int().min(50, "Minimum 50 ms").max(300000),
});
