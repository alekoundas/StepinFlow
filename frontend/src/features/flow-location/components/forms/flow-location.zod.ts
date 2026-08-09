import { z } from "zod";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

export const FlowLocationZod = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),

  flowSearchAreaId: z.number().int().nullish(),
  offsetMode: z.enum(AreaSizingModeEnum),

  locationX: z.number().int("X must be a whole pixel"),
  locationY: z.number().int("Y must be a whole pixel"),

  ratioX: z.number().min(0).max(1),
  ratioY: z.number().min(0).max(1),
});
