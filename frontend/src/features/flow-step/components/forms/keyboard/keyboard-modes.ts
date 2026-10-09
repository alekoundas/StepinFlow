import { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import { KeyboardKeyActionTypeEnum } from "@/shared/enums/backend/keyboard-key-action-type-enum";

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

export interface KeyAction {
  value: KeyboardKeyActionTypeEnum;
  label: string;
  description: string;
}

/**
 * What Send keys does with its keys. Hold and Release are two halves of one gesture, the way a
 * click's are: whatever runs between them happens with the keys down.
 */
export const KEY_ACTIONS: KeyAction[] = [
  {
    value: KeyboardKeyActionTypeEnum.PRESS,
    label: "Press",
    description: "Presses the keys and lets them go, the way a shortcut arrives.",
  },
  {
    value: KeyboardKeyActionTypeEnum.HOLD,
    label: "Hold",
    description:
      "Puts the keys down and leaves them down for the steps after it - Hold Ctrl, click, Release Ctrl is a Ctrl+click. Anything still held when the execution ends is let go.",
  },
  {
    value: KeyboardKeyActionTypeEnum.RELEASE,
    label: "Release",
    description: "Lets go of keys a Hold put down.",
  },
];

export const KEY_ACTION_VALUES = KEY_ACTIONS.map((x) => x.value) as [
  KeyboardKeyActionTypeEnum,
  ...KeyboardKeyActionTypeEnum[],
];
