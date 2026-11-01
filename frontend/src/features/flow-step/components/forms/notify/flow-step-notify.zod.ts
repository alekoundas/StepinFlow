import { z } from "zod";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepNotifySchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,
    discordBotId: z.number().int().nullish(),

    /** Optional, always. A message with only the flow name is still a message. */
    message: z
      .string()
      .max(1500, "Discord will not take a message this long")
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),

    /** Which step's result to include. Unset means "just send my message". */
    flowStepReferenceId: z.number().int().nullish(),
  })
  .superRefine((data, ctx) => {
    if (!data.discordBotId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the bot to send through",
        path: ["discordBotId"],
      });
    }
  });
