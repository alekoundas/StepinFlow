import { z } from "zod";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export const FlowStepImageSearchSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    searchMode: z.enum(SearchModeEnum),
    flowAreaId: z.number().int().nullish(),

    templateMatchMode: z.enum(TemplateMatchModeEnum),
    accuracy: z.number().min(0.1).max(1),
    maxMatches: z.number().int().min(1).max(2147483647),

    pollIntervalMilliseconds: z.number().int().min(0).max(2147483647),
    timeoutMilliseconds: z.number().int().min(0).max(2147483647),

    flowStepImages: z.array(z.any()).optional(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowAreaId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick where to look",
        path: ["flowAreaId"],
      });
    }

    if (data.searchMode !== SearchModeEnum.FIND_BEST) {
      if (data.pollIntervalMilliseconds < 50) {
        ctx.addIssue({
          code: "custom",
          message: "Polling faster than 50ms just burns CPU",
          path: ["pollIntervalMilliseconds"],
        });
      }
    }
  });
