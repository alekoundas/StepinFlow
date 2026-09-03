import { z } from "zod";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

export const FlowPointZod = z.object({
  // Kept in the schema on purpose: zod strips unknown keys, so leaving it out sent the
  // flow save a point with no id, which reads as new - the old row was deleted and every
  // step pointing at it had its FlowPointId nulled.
  id: z.number().int(),
  name: z.string().min(1, "Name is required").max(120, "Name too long"),

  flowAreaId: z.number().int().nullish(),
  offsetMode: z.enum(AreaSizingModeEnum),

  locationX: z.number().int("X must be a whole pixel"),
  locationY: z.number().int("Y must be a whole pixel"),

  // Stored 0..1, shown as 0..100 %, so the messages talk in percent.
  ratioX: z
    .number()
    .min(0, "X must be between 0% and 100% of the area")
    .max(1, "X must be between 0% and 100% of the area"),
  ratioY: z
    .number()
    .min(0, "Y must be between 0% and 100% of the area")
    .max(1, "Y must be between 0% and 100% of the area"),
});
