import { z } from "zod";
import { FlowSchema } from "@/features/flow/components/form/flow.zod";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";

const noScriptBrackets = (text: string) => !text.includes("<[") && !text.includes("]>");

// What a window is resized to, title bar and borders included.
const MAXIMUM_WINDOW_EDGE = 16384;

// The window a recording works in, which becomes the flow's main area: an application's window,
// or a website's tab in a browser.
const WINDOW_SHAPE = {
  areaType: z.enum([FlowAreaTypeEnum.APPLICATION, FlowAreaTypeEnum.BROWSER_TAB]),
  processName: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
  titlePattern: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
  titleMatchMode: z.enum(TitleMatchModeEnum),
  tabAddress: z.string().refine(noScriptBrackets, "Can't contain <[ or ]>"),
  ignoreWindowBorders: z.boolean(),
  scalesWith: z.enum(ScalesWithEnum).nullish(),
};

function refineWindow(
  data: {
    areaType: FlowAreaTypeEnum;
    processName: string;
    titlePattern: string;
    tabAddress: string;
    scalesWith?: ScalesWithEnum | null;
  },
  ctx: z.RefinementCtx,
) {
  // A tab is found by its browser and its address: a browser's title is its front tab's.
  if (data.areaType === FlowAreaTypeEnum.BROWSER_TAB) {
    if (data.processName.length === 0) {
      ctx.addIssue({ code: "custom", message: "Pick the browser", path: ["processName"] });
    }

    if (data.tabAddress.trim().length === 0) {
      ctx.addIssue({ code: "custom", message: "Type the website's address", path: ["tabAddress"] });
    }

    return;
  }

  // Matching on nothing would take the first window on the desktop.
  if (data.processName.length === 0 && data.titlePattern.length === 0) {
    ctx.addIssue({
      code: "custom",
      message: "Pick an application, or type a title to match",
      path: ["processName"],
    });
  }

  // A native app and a game look the same from outside, so an application asks.
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
    addsResizeStep: z.boolean(),
    windowWidth: z.number().int().min(0).max(MAXIMUM_WINDOW_EDGE),
    windowHeight: z.number().int().min(0).max(MAXIMUM_WINDOW_EDGE),
  })
  .superRefine((data, ctx) => {
    refineWindow(data, ctx);

    if (!data.addsResizeStep) return;

    if (data.windowWidth < 1) {
      ctx.addIssue({ code: "custom", message: "Give the window a width", path: ["windowWidth"] });
    }

    if (data.windowHeight < 1) {
      ctx.addIssue({ code: "custom", message: "Give the window a height", path: ["windowHeight"] });
    }
  });

export type RecordingWindowForm = z.infer<typeof RecordingWindowSchema>;
export type RecordingSetupForm = z.infer<typeof RecordingSetupSchema>;
