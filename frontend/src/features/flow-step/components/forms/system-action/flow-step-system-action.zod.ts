import { z } from "zod";
import { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepSystemActionSchema = z.object({
  codeComment: CODE_COMMENT_SCHEMA,
  name: z
    .string()
    .min(1, "Name is required")
    .max(120, "Name too long")
    .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
  systemActionType: z.enum(SystemActionTypeEnum),
});
