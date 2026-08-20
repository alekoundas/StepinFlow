import { z } from "zod";
import { READ_TEXT_CONDITION_TYPES } from "@/features/flow-step/components/forms/shared/condition-types";
import {
  isWaitingMode,
  READ_TEXT_MODE_VALUES,
} from "@/features/flow-step/components/forms/shared/search-modes";

export const FlowStepReadTextSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    flowAreaId: z.number().int().nullish(),
    ocrLanguage: z.string().min(1, "Pick the language the text is written in"),

    conditionText: z.string(),
    conditionType: z.enum(READ_TEXT_CONDITION_TYPES),

    searchMode: z.enum(READ_TEXT_MODE_VALUES),
    pollIntervalMilliseconds: z.number().int().min(50),
    timeoutMilliseconds: z.number().int().min(0),

    resultExtractPattern: z.string(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowAreaId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick where on screen to read",
        path: ["flowAreaId"],
      });
    }

    // Reading once succeeds on having read anything, so only the waiting modes need a condition.
    if (isWaitingMode(data.searchMode) && data.conditionText.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Type the text to wait for",
        path: ["conditionText"],
      });
    }
  });
