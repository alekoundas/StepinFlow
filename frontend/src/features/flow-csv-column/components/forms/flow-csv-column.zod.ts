import { z } from "zod";

export const FlowCsvColumnZod = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Name is required")
    .max(60, "Name too long")
    .refine((x) => !x.includes("{") && !x.includes("}"), "Without braces: a step adds those around it"),
  defaultValue: z.string().max(500, "Keep it under 500 characters"),
  isSecret: z.boolean(),
});
