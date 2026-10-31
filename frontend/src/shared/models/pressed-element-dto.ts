/**
 * What a press landed on, as its application described it to Windows at that moment: a button, a
 * link, a text field, with its name and state. None for an application that draws its own pixels,
 * a game.
 */
export interface PressedElementDto {
  /** The click, drag or check it belongs to, by its index among the recording's actions. */
  actionIndex: number;

  /** Button, link, text field, check box, in English. */
  role: string;

  name: string;
  isEnabled: boolean;

  /** Ticked or not, for something that can be. */
  isChecked?: boolean | null;

  /** A field whose text is hidden as it is typed. */
  isPassword: boolean;

  /** What it sits in, nearest first, each its role and name: document "Swag Labs". */
  inside: string[];
}
