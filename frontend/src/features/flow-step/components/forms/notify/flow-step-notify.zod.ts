import { z } from "zod";

export const FlowStepNotifySchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    discordBotId: z.number().int().nullish(),

    /** Optional, always. A message with only the flow name is still a message. */
    notifyMessage: z.string().max(1500, "Discord will not take a message this long"),

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
