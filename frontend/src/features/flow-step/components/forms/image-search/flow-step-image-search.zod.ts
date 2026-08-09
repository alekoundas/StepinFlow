import { z } from "zod";
import { ImageSearchModeEnum } from "@/shared/enums/backend/image-search-mode-enum";
import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export const FlowStepImageSearchSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    imageSearchMode: z.enum(ImageSearchModeEnum),
    flowSearchAreaId: z.number().int().nullish(),

    templateMatchMode: z.enum(TemplateMatchModeEnum),
    accuracy: z.number().min(0.1).max(1),
    maxMatches: z.number().int().min(1).max(2147483647),
    loopOnMultipleFindings: z.boolean(),

    pollIntervalMilliseconds: z.number().int().min(0).max(2147483647),
    timeoutMilliseconds: z.number().int().min(0).max(2147483647),
  })
  .superRefine((data, ctx) => {
    if (!data.flowSearchAreaId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick where to look",
        path: ["flowSearchAreaId"],
      });
    }

    if (data.imageSearchMode !== ImageSearchModeEnum.FIND_ONCE) {
      if (data.pollIntervalMilliseconds < 50) {
        ctx.addIssue({
          code: "custom",
          message: "Polling faster than 50ms just burns CPU",
          path: ["pollIntervalMilliseconds"],
        });
      }
    }
  });
