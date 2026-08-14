import { z } from "zod";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { ImageSearchModeEnum } from "@/shared/enums/backend/image-search-mode-enum";

// Matching a whole block of read text against nothing is not a search, so only these make sense.
export const TEXT_SEARCH_CONDITION_TYPES = [
  ConditionTypeEnum.CONTAINS,
  ConditionTypeEnum.EQUALS,
  ConditionTypeEnum.MATCHES_REGEX,
] as const;

export const FlowStepTextSearchSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    flowAreaId: z.number().int().nullish(),
    ocrLanguage: z.string().min(1, "Pick the language the text is written in"),

    conditionText: z.string(),
    conditionType: z.enum(TEXT_SEARCH_CONDITION_TYPES),

    imageSearchMode: z.enum(ImageSearchModeEnum),
    pollIntervalMilliseconds: z.number().int().min(50),
    timeoutMilliseconds: z.number().int().min(0),
    loopOnMultipleFindings: z.boolean(),

    resultVariableName: z
      .string()
      .max(60, "Name too long")
      .regex(/^[A-Za-z0-9_]*$/, "Letters, numbers and underscores only"),
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

    if (data.conditionText.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Type the text to look for",
        path: ["conditionText"],
      });
    }
  });
