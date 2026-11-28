import { z } from "zod";

import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KeyboardModes } from "@/features/flow-step/components/forms/keyboard/keyboard-modes";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

export const FlowStepKeyboardSchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,

    keyboardInputType: z.enum(KeyboardModes.KEYBOARD_MODE_VALUES),
    keyboardKeyActionType: z.enum(KeyboardModes.KEY_ACTION_VALUES).nullish(),
    keyboardInputText: z
      .string()
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
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
