// A recording gesture ends with the mouse button, in the backend's own name for it.
const BUTTON_NAMES: Record<string, string> = {
  LEFT_BUTTON: "Click",
  RIGHT_BUTTON: "Right click",
  MIDDLE_BUTTON: "Middle click",
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
