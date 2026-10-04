import { z } from "zod";
import { scriptName, scriptText } from "@/shared/utils/script-text";

export const FlowStepEndExecutionSchema = z
  .object({
    name: scriptName(),

    endExecutionAsSuccess: z.boolean(),
    message: scriptText(z.string().max(1500, "Too long to be a useful reason")),
  })
  .superRefine((data, ctx) => {
    // A pass needs no explanation. A failure is the line that lands in the report, and "it failed"
    // helps nobody at eight in the morning looking at a red build.
    if (data.endExecutionAsSuccess || data.message.trim().length > 0) return;

    ctx.addIssue({
      code: "custom",
      message: "Say what went wrong - this is what the report shows",
      path: ["message"],
    });
  });
