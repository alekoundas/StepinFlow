/**
 * What changed on the main area between one press's screenshot and the next, by the text OCR
 * read: what the action, and any up to the next press, made appear and go.
 */
export interface ScreenChangeDto {
  /** The action whose press the change is measured from, by its index. */
  actionIndex: number;

  /** The next action with a press, whose screenshot it is measured to. Null for the screen the recording stopped on. */
  nextActionIndex: number | null;

  /** Lines read on the second screenshot and not the first, in the order read. */
  appeared: string[];

  /** Lines read on the first screenshot and not the second. */
  gone: string[];

  /** How much of the text changed, from 0 to 1. */
  changedShare: number;

  /** Most of the text changed: a new page or a dialog. */
  isNewScreen: boolean;

  /** The main area's window has another title at the second press: for a browser, another page. */
  isNewTitle: boolean;

  /** The window's title at the second press, when it was pressed in the main area. */
  title?: string | null;
}
