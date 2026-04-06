import { CursorActionTypeEnum } from "@/shared/enums/backend/cursor-action-type-enum";
import { z } from "zod";

export const FlowStepCursorDragSchema = z.object({
  name: z.string().min(1, "Name is required").max(120, "Name too long"),
  cursorActionType: z.enum(CursorActionTypeEnum, {
    error: () => ({
      message: "Please select a cursor action type",
    }),
  }),
});
