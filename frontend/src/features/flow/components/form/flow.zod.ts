// schemas/base-flow-step.schema.ts
import { FlowAreaZod } from "@/features/flow-area/components/forms/flow-area.zod";
import { FlowPointZod } from "@/features/flow-point/components/forms/flow-point.zod";
import { FlowViewportZod } from "@/features/flow-viewport/components/forms/flow-viewport.zod";
import { z } from "zod";
import { AppCloseModeEnum } from "@/shared/enums/backend/app-close-mode-enum";

const APP_CLOSE_MODE_VALUES = Object.values(AppCloseModeEnum) as [
  AppCloseModeEnum,
  ...AppCloseModeEnum[],
];

export const FlowSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  description: z.string().max(300, "Keep it to a line"),

  /** The area bound to the application under test. Unset until a flow has one. */
  appUnderTestAreaId: z.number().int().nullish(),
  appCloseMode: z.enum(APP_CLOSE_MODE_VALUES),

  flowAreas: z.array(FlowAreaZod),
  flowPoints: z.array(FlowPointZod),
  flowViewports: z.array(FlowViewportZod),
});
