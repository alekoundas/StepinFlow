import { z } from "zod";
import {
  needsSecondValue,
  needsValue,
  SEARCH_TEXT_CONDITION_TYPES,
} from "@/features/flow-step/components/forms/shared/condition-types";
import { SEARCH_TEXT_MODE_VALUES } from "@/features/flow-step/components/forms/shared/search-modes";

export const FlowStepSearchTextSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    flowAreaId: z.number().int().nullish(),
    ocrLanguage: z.string().min(1, "Pick the language the text is written in"),

    conditionText: z.string(),
    conditionTextEnd: z.string(),
    conditionType: z.enum(SEARCH_TEXT_CONDITION_TYPES),

    searchMode: z.enum(SEARCH_TEXT_MODE_VALUES),
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

    // Every mode decides now, so the condition is never optional. Only the two that compare
    // against nothing - is empty, is not empty - have no value to ask for.
    if (needsValue(data.conditionType) && data.conditionText.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Type what has to be true of the text",
        path: ["conditionText"],
      });
    }

    if (needsSecondValue(data.conditionType) && data.conditionTextEnd.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Type the upper bound",
        path: ["conditionTextEnd"],
      });
    }
  });
