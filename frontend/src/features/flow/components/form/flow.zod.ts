// schemas/base-flow-step.schema.ts
import { FlowSearchAreaZod } from "@/features/flow-search-area/components/forms/flow-search-area.zod";
import { FlowLocationZod } from "@/features/flow-location/components/forms/flow-location.zod";
import { z } from "zod";

export const FlowSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  orderNumber: z
    .number()
    .int()
    .min(0, "Order must be >= 0")
    .max(2147483647, "Order too large"),
  flowSearchAreas: z.array(FlowSearchAreaZod),
  flowLocations: z.array(FlowLocationZod),
});
