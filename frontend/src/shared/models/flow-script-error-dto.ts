export interface FlowScriptErrorDto {
  line: number;
  column: number;
  message: string;

  /** The message is for reading; these are for deciding what to do about it. */
  code: string;
  severity: string;
}
