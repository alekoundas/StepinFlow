import { z } from "zod";
import { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { BrowserTypeEnum } from "@/shared/enums/backend/area/browser-type-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";

export const FlowSearchAreaZod = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    type: z.enum(FlowSearchAreaTypeEnum),

    parentFlowSearchAreaId: z.number().int().nullish(),
    sizingMode: z.enum(AreaSizingModeEnum),

    locationX: z.number().int(),
    locationY: z.number().int(),
    width: z.number().int(),
    height: z.number().int(),

    ratioX: z.number().min(0).max(1),
    ratioY: z.number().min(0).max(1),
    ratioWidth: z.number().min(0).max(1),
    ratioHeight: z.number().min(0).max(1),

    processName: z.string(),
    titlePattern: z.string(),
    titleMatchMode: z.enum(TitleMatchModeEnum),
    instanceIndex: z.number().int().min(0),
    useClientArea: z.boolean(),

    browserType: z.enum(BrowserTypeEnum),
    tabMatchValue: z.string(),
    tabMatchOn: z.enum(TabMatchOnEnum),

    monitorUniqueId: z.string(),
  })
  .superRefine((data, ctx) => {
    if (data.type === FlowSearchAreaTypeEnum.CUSTOM) {
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
      data.type === FlowSearchAreaTypeEnum.APPLICATION ||
      data.type === FlowSearchAreaTypeEnum.BROWSER_TAB
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

    if (data.type === FlowSearchAreaTypeEnum.BROWSER_TAB) {
      if (data.tabMatchValue.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Type the tab title or URL to look for",
          path: ["tabMatchValue"],
        });
      }
    }

    if (data.type === FlowSearchAreaTypeEnum.MONITOR) {
      if (data.monitorUniqueId.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Monitor is required",
          path: ["monitorUniqueId"],
        });
      }
    }
  });
