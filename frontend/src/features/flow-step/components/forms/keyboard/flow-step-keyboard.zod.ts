import { z } from "zod";

import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KEYBOARD_MODE_VALUES } from "@/features/flow-step/components/forms/keyboard/keyboard-modes";

export const FlowStepKeyboardSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    keyboardInputType: z.enum(KEYBOARD_MODE_VALUES),
    keyboardInputText: z.string(),
  })
  .superRefine((data, ctx) => {
    if (data.keyboardInputText.length > 0) return;

    ctx.addIssue({
      code: "custom",
      message:
        data.keyboardInputType === KeyboardInputTypeEnum.COMBINATION
          ? "Record the keys to send"
          : "Type the text this step should enter",
      path: ["keyboardInputText"],
    });
  });
