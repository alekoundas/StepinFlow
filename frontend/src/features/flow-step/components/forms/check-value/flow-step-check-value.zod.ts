import { z } from "zod";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import {
  needsSecondValue,
  needsValue,
} from "@/features/flow-step/components/forms/shared/condition-types";

export const FlowStepCheckValueSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    flowStepReferenceId: z.number().int().nullish(),
    conditionType: z.enum(ConditionTypeEnum),
    conditionText: z.string(),
    conditionTextEnd: z.string(),
  })
  .superRefine((data, ctx) => {
    if (!data.flowStepReferenceId) {
      ctx.addIssue({
        code: "custom",
        message: "Pick the step whose result is checked",
        path: ["flowStepReferenceId"],
      });
    }

    if (needsValue(data.conditionType) && data.conditionText.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Type what to check the result against",
        path: ["conditionText"],
      });
    }

    if (needsSecondValue(data.conditionType) && data.conditionTextEnd.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "A range needs both ends",
        path: ["conditionTextEnd"],
      });
    }
  });
