import { z } from "zod";

export const FlowStepMarkerSchema = z.object({
  name: z
    .string()
    .min(1, "Name is required")
    .max(120, "Name too long")
    .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
});
