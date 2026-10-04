import { z } from "zod";

import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KEYBOARD_MODE_VALUES } from "@/features/flow-step/components/forms/keyboard/keyboard-modes";
import { scriptName, scriptText } from "@/shared/utils/script-text";

export const FlowStepKeyboardSchema = z
  .object({
    name: scriptName(),

    keyboardInputType: z.enum(KEYBOARD_MODE_VALUES),
    keyboardInputText: scriptText(),
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
