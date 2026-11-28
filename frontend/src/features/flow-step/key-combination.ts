/**
 * Both sides of a key are the same shortcut to a reader, and the backend resolves an unqualified
 * modifier to the left one.
 */
const MODIFIER_NAMES: Record<string, string> = {
  LeftCtrl: "Ctrl",
  RightCtrl: "Ctrl",
  LeftAlt: "Alt",
  RightAlt: "Alt",
  LeftShift: "Shift",
  RightShift: "Shift",
  LeftMeta: "Win",
  RightMeta: "Win",
};

// The order KeyCombinationHelper reads them back in, so a captured combination and a recorded one
// are the same string.
const MODIFIER_ORDER = ["Ctrl", "Alt", "Shift", "Win"];

/**
 * The text form of what was held, or null when nothing but modifiers was - unless modifiers alone
 * are wanted, as a Hold of Ctrl is.
 *
 * Key names are KeyCodeEnum members untranslated, because that is what the backend parses back
 * into keys. Anything invented here would be a combination the engine cannot press.
 */
const toCombination = (keyCodes: string[], allowModifiersOnly = false): string | null => {
  const pressed = keyCodes.filter((x) => !(x in MODIFIER_NAMES));
  const modifiers = MODIFIER_ORDER.filter((name) =>
    keyCodes.some((x) => MODIFIER_NAMES[x] === name),
  );

  if (pressed.length === 0) {
    if (allowModifiersOnly && modifiers.length > 0) return modifiers.join("+");

    return null;
  }

  // The last one, so rolling off a combination binds the key that finished it.
  return [...modifiers, pressed[pressed.length - 1]].join("+");
};

/** What to show while keys are still down. Modifiers only is a legitimate half-way state. */
const toDisplay = (keyCodes: string[]): string =>
  keyCodes.map((x) => MODIFIER_NAMES[x] ?? x).join("+");

export const KeyCombination = {
  toCombination,
  toDisplay,
};
