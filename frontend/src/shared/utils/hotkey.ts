/**
 * A hotkey is stored in the hook's vocabulary - "VcLeftControl+VcF10" - because that is what gets
 * matched. The Vc prefix is the hook's, not something a person should have to read.
 */
export const toHotkeyDisplay = (combination: string): string =>
  combination
    .split("+")
    .map((part) => (part.startsWith("Vc") ? part.slice(2) : part))
    .join(" + ");
