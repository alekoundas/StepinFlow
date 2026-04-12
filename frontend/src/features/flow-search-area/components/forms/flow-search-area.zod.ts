import { z } from "zod";
import { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";

export const FlowSearchAreaZod = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),
    type: z.enum(FlowSearchAreaTypeEnum),
    applicationName: z.string(),
    monitorName: z.string(),
    locationX: z.number(),
    locationY: z.number(),
    width: z.number(),
    height: z.number(),
  })
  .superRefine((data, ctx) => {
    if (data.type === "CUSTOM") {
      // if (
      //   data.locationX === -1 ||
      //   data.locationY === -1 ||
      //   data.width === -1 ||
      //   data.height === -1
      // ) {
      //   ctx.addIssue({
      //     code: "custom",
      //     message:
      //       "All location and size parameters are required for custom search areas",
      //     path: ["locationX"],
      //   });
      // }
    }
    if (data.type === "APPLICATION") {
      if (data.applicationName.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Application Name is required",
          path: ["applicationName"],
        });
      }
    }
    if (data.type === "MONITOR") {
      if (data.monitorName.length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Monitor Name is required",
          path: ["monitorName"],
        });
      }
    }
  });
