import { z } from "zod";

export const FlowLocationZod = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  locationX: z.number().int("X must be a whole pixel"),
  locationY: z.number().int("Y must be a whole pixel"),
});
