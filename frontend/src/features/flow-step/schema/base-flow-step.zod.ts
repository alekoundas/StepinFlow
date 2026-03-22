// schemas/base-flow-step.schema.ts
import { z } from "zod";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export const BaseFlowStepSchema = z.object({
  id: z.number().int().min(-1), // -1 = new
  name: z.string().min(1, "Name is required").max(120),
  flowStepType: z.enum(FlowStepTypeEnum),
  orderNumber: z.number().int().min(0),

  locationX: z.number().int(),
  locationY: z.number().int(),
  locationEndX: z.number().int(),
  locationEndY: z.number().int(),

  // relations (usually optional / lazy-loaded)
  flowId: z.number().int().optional(),
  subFlowId: z.number().int().optional(),
  flowSearchAreaId: z.number().int().optional(),
  parentFlowStepId: z.number().int().optional(),
  flowStepReferenceId: z.number().int().optional(),

  childrenFlowSteps: z.array(z.any()).optional(),
  flowStepReferences: z.array(z.any()).optional(),
  flowStepImages: z.array(z.any()).optional(),
});
