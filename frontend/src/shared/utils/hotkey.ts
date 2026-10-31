// A recording gesture ends with the mouse button, in the backend's own name for it.
const BUTTON_NAMES: Record<string, string> = {
  LEFT_BUTTON: "Click",
  RIGHT_BUTTON: "Right click",
  MIDDLE_BUTTON: "Middle click",
};

type ModifierFlag = "ctrlKey" | "shiftKey" | "altKey" | "metaKey";

const MODIFIER_FLAGS: Record<string, ModifierFlag> = {
  VcLeftControl: "ctrlKey",
  VcRightControl: "ctrlKey",
  VcLeftShift: "shiftKey",
  VcRightShift: "shiftKey",
  VcLeftAlt: "altKey",
  VcRightAlt: "altKey",
  VcLeftMeta: "metaKey",
  VcRightMeta: "metaKey",
};

/**
 * Whether a key pressed in the page is a stored hotkey. The function keys, letters and digits read
 * the same in both vocabularies; a mouse button never matches a key.
 */
export const matchesHotkey = (event: KeyboardEvent, combination: string | undefined): boolean => {
  if (!combination) return false;

  const parts = combination.split("+");
  const key = parts.find((x) => !(x in MODIFIER_FLAGS));
  if (!key?.startsWith("Vc")) return false;

  const wanted = new Set(parts.filter((x) => x in MODIFIER_FLAGS).map((x) => MODIFIER_FLAGS[x]));
  const flags: ModifierFlag[] = ["ctrlKey", "shiftKey", "altKey", "metaKey"];
  if (flags.some((x) => event[x] !== wanted.has(x))) return false;

  return key.slice(2) === event.code.replace(/^(Key|Digit)/, "");
};

/**
 * A hotkey is stored in the hook's vocabulary - "VcLeftControl+VcF10" - because that is what gets
 * matched. The Vc prefix is the hook's, not something a person should have to read.
 */
export const toHotkeyDisplay = (combination: string): string =>
  combination
    .split("+")
    .map((part) => BUTTON_NAMES[part] ?? (part.startsWith("Vc") ? part.slice(2) : part))
    .join(" + ");
