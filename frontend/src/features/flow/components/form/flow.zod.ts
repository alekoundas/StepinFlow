// schemas/base-flow-step.schema.ts
import { FlowAreaZod } from "@/features/flow-area/components/forms/flow-area.zod";
import { FlowPointZod } from "@/features/flow-point/components/forms/flow-point.zod";
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

  flowAreas: z.array(FlowAreaZod),
  flowPoints: z.array(FlowPointZod),
  flowViewports: z.array(FlowViewportZod),
});
