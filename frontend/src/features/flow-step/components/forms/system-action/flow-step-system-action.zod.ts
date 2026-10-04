import { z } from "zod";
import { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";
import { scriptName } from "@/shared/utils/script-text";

export const FlowStepSystemActionSchema = z.object({
  name: scriptName(),
  systemActionType: z.enum(SystemActionTypeEnum),
});
