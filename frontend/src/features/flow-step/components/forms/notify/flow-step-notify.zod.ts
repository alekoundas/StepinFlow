import { z } from "zod";
import { scriptName, scriptText } from "@/shared/utils/script-text";

export const FlowStepNotifySchema = z
  .object({
    name: scriptName(),
    discordBotId: z.number().int().nullish(),

    /** Optional, always. A message with only the flow name is still a message. */
    message: scriptText(z.string().max(1500, "Discord will not take a message this long")),

    /** Which failed step to describe. Unset means "just send my message". */
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
