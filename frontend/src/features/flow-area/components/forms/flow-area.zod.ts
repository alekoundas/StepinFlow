import { z } from "zod";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";

export const FlowAreaZod = z
  .object({
    // Kept in the schema on purpose: zod strips unknown keys, so leaving it out sent
    // the flow save an area with no id, which reads as new - the old row was deleted
    // and every step pointing at it had its FlowAreaId nulled.
    id: z.number().int(),
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    type: z.enum(FlowAreaTypeEnum),

    scalesWith: z.enum(ScalesWithEnum).nullish(),
    authoredDpi: z.number().int(),

    parentFlowAreaId: z.number().int().nullish(),
    sizingMode: z.enum(AreaSizingModeEnum),

    locationX: z.number().int(),
    locationY: z.number().int(),
    width: z.number().int(),
    height: z.number().int(),

    // Stored 0..1, shown as 0..100 %, so the messages talk in percent.
    ratioX: z.number().min(0, "X must be 0% to 100%").max(1, "X must be 0% to 100%"),
    ratioY: z.number().min(0, "Y must be 0% to 100%").max(1, "Y must be 0% to 100%"),
    ratioWidth: z
      .number()
      .min(0, "Width must be 0% to 100%")
      .max(1, "Width must be 0% to 100%"),
    ratioHeight: z
      .number()
      .min(0, "Height must be 0% to 100%")
      .max(1, "Height must be 0% to 100%"),

    processName: z.string(),
    titlePattern: z.string(),
    titleMatchMode: z.enum(TitleMatchModeEnum),
    useClientArea: z.boolean(),

    tabMatchValue: z.string(),
    tabMatchOn: z.enum(TabMatchOnEnum),

    monitorDeviceName: z.string(),
  })
  .superRefine((data, ctx) => {
    if (data.type === FlowAreaTypeEnum.CUSTOM) {
      if (data.sizingMode === AreaSizingModeEnum.RATIO) {
        if (data.ratioWidth <= 0 || data.ratioHeight <= 0) {
          ctx.addIssue({
            code: "custom",
            message: "Capture or type a size",
            path: ["ratioWidth"],
          });
        }
      } else if (data.width <= 0 || data.height <= 0) {
        ctx.addIssue({
          code: "custom",
          message: "Capture or type a size",
          path: ["width"],
        });
      }
    }

    if (
      data.type === FlowAreaTypeEnum.APPLICATION ||
      data.type === FlowAreaTypeEnum.BROWSER_TAB
    ) {
      // Either is enough on its own, but matching on nothing would take the first window
      // on the desktop.
      if (data.processName.length === 0 && data.titlePattern.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Pick an application, or type a title to match",
          path: ["processName"],
        });
      }
    }

    // A native app and a game look the same from outside, so this is the one type that asks.
    if (data.type === FlowAreaTypeEnum.APPLICATION && !data.scalesWith) {
      ctx.addIssue({
        code: "custom",
        message: "Pick what its contents scale with",
        path: ["scalesWith"],
      });
    }

    if (data.type === FlowAreaTypeEnum.BROWSER_TAB) {
      if (data.tabMatchValue.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Type the tab title or URL to look for",
          path: ["tabMatchValue"],
        });
      }
    }

  });
