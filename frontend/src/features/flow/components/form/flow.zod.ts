// schemas/base-flow-step.schema.ts
import { FlowAreaZod } from "@/features/flow-area/components/forms/flow-area.zod";
import { FlowPointZod } from "@/features/flow-point/components/forms/flow-point.zod";
import { z } from "zod";

export const FlowSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  description: z.string().max(300, "Keep it to a line"),
  flowAreas: z.array(FlowAreaZod),
  flowPoints: z.array(FlowPointZod),
});
