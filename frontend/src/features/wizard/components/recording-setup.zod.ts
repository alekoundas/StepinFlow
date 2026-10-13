import { z } from "zod";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

const noScriptBrackets = (text: string) => !text.includes("<[") && !text.includes("]>");

export const RecordingSetupSchema = z
  .object({
    name: FlowSchema.shape.name,
    description: FlowSchema.shape.description,
    isQaTest: z.boolean(),

    processName: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
    titlePattern: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
    titleMatchMode: z.enum(TitleMatchModeEnum),
    useClientArea: z.boolean(),
    scalesWith: z.enum(ScalesWithEnum).nullish(),

    addsLaunchStep: z.boolean(),
  })
  .superRefine((data, ctx) => {
    // Matching on nothing would take the first window on the desktop.
    if (data.processName.length === 0 && data.titlePattern.length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "Pick an application, or type a title to match",
        path: ["processName"],
      });
    }

    if (!data.scalesWith) {
      ctx.addIssue({
        code: "custom",
        message: "Pick what its contents scale with",
        path: ["scalesWith"],
      });
    }
  });

export type RecordingSetupForm = z.infer<typeof RecordingSetupSchema>;
