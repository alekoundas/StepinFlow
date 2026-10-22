// schemas/base-flow-step.schema.ts
import { FlowViewportZod } from "@/features/flow-viewport/components/forms/flow-viewport.zod";
import { validateFileName } from "@/shared/utils/file-name";
import { z } from "zod";

// Room for what a model needs to remember about the flow, not only a line for the list.
export const DESCRIPTION_MAX_LENGTH = 5000;

export const FlowSchema = z.object({
  name: z
    .string()
    .min(1, "Name is required")
    .max(120, "Name too long")
    .superRefine((text, ctx) => {
      const problem = validateFileName(text);
      if (problem) ctx.addIssue({ code: "custom", message: problem });
    }),
  description: z.string().max(DESCRIPTION_MAX_LENGTH, `Keep it under ${DESCRIPTION_MAX_LENGTH} characters`),

  // Areas, locations and inputs have tabs of their own and are saved one at a time.
  flowViewports: z.array(FlowViewportZod),
});
