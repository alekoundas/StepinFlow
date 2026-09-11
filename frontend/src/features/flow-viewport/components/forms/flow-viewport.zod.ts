import { z } from "zod";

export const FlowViewportZod = z.object({
  id: z.number().int(),
  width: z.number().int().min(1, "Give it a width"),
  height: z.number().int().min(1, "Give it a height"),
  orderNumber: z.number().int(),
  flowId: z.number().int(),
});
