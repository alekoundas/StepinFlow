import { z } from "zod";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

const noScriptBrackets = (text: string) => !text.includes("<[") && !text.includes("]>");

// The window a recording works in, which becomes the flow's main area.
const WINDOW_SHAPE = {
  processName: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
  titlePattern: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
  titleMatchMode: z.enum(TitleMatchModeEnum),
  useClientArea: z.boolean(),
  scalesWith: z.enum(ScalesWithEnum).nullish(),
};

function refineWindow(
  data: { processName: string; titlePattern: string; scalesWith?: ScalesWithEnum | null },
  ctx: z.RefinementCtx,
) {
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
}

export const RecordingWindowSchema = z.object(WINDOW_SHAPE).superRefine(refineWindow);

export const RecordingSetupSchema = z
  .object({
    name: FlowSchema.shape.name,
    description: FlowSchema.shape.description,
    isQaTest: z.boolean(),
    ...WINDOW_SHAPE,
    addsLaunchStep: z.boolean(),
  })
  .superRefine(refineWindow);

export type RecordingWindowForm = z.infer<typeof RecordingWindowSchema>;
export type RecordingSetupForm = z.infer<typeof RecordingSetupSchema>;
