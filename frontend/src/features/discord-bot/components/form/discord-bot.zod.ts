import { z } from "zod";

import {
  RATE_LIMIT_MAX_SECONDS,
  RATE_LIMIT_MIN_SECONDS,
} from "@/shared/models/database/discord-bot-dto";

/** Discord's own webhook host. Anything else is a pasted mistake rather than a webhook. */
const WEBHOOK_PATTERN = /^https:\/\/(canary\.|ptb\.)?discord(app)?\.com\/api\/webhooks\/\d+\/.+/;

export const DiscordBotSchema = z.object({
  name: z.string().min(1, "Give it a name").max(80, "Name too long"),

  webhookUrl: z
    .string()
    .min(1, "Paste the webhook URL")
    .regex(
      WEBHOOK_PATTERN,
      "That is not a Discord webhook URL. Copy it from the channel's Integrations settings.",
    ),

  botName: z.string().max(80, "Too long for a Discord name"),

  avatarUrl: z
    .string()
    .max(500, "Too long")
    .refine((x) => x === "" || /^https?:\/\//.test(x), {
      message: "Discord fetches this itself, so it has to be a link, not a file",
    }),

  rateLimitSeconds: z
    .number()
    .int()
    .min(RATE_LIMIT_MIN_SECONDS, `At least ${RATE_LIMIT_MIN_SECONDS} seconds`)
    .max(RATE_LIMIT_MAX_SECONDS, `At most ${RATE_LIMIT_MAX_SECONDS} seconds`),
});
