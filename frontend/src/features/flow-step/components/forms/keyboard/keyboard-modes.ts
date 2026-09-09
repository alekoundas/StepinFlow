import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";

export interface KeyboardMode {
  value: KeyboardInputTypeEnum;
  label: string;
  description: string;
  defaultName: string;
}

/**
 * The two are not the same thing said differently: "Ctrl+V" typed as text puts six characters into
 * whatever has focus, where pressed as keys it pastes.
 */
export const KEYBOARD_MODES: KeyboardMode[] = [
  {
    value: KeyboardInputTypeEnum.TEXT,
    label: "Type text",
    description: "Types the characters one at a time, as though someone were at the keyboard.",
    defaultName: "Type Text",
  },
  {
    value: KeyboardInputTypeEnum.COMBINATION,
    label: "Send keys",
    description:
      "Holds the modifiers and presses the key, the way a shortcut is meant to arrive. Pressing Enter or Tab is its own step.",
    defaultName: "Send Keys",
  },
];

export const KEYBOARD_MODE_VALUES = KEYBOARD_MODES.map((x) => x.value) as [
  KeyboardInputTypeEnum,
  ...KeyboardInputTypeEnum[],
];
