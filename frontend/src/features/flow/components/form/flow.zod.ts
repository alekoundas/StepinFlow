// schemas/base-flow-step.schema.ts
import { FlowAreaZod } from "@/features/flow-area/components/forms/flow-area.zod";
import { FlowPointZod } from "@/features/flow-point/components/forms/flow-point.zod";
import { z } from "zod";

export const FlowSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  orderNumber: z
    .number()
    .int()
    .min(0, "Order must be >= 0")
    .max(2147483647, "Order too large"),
  flowAreas: z.array(FlowAreaZod),
  flowPoints: z.array(FlowPointZod),
});
