/** The Check page's "Ask for changes": the flow, and what the tester wrote. */
export interface RecordingDraftRevisionDto {
  flowId: number;
  changes: string;

  /** The proposed checks not yet kept or dropped, by name, so the answer still offers them. */
  undecided: string[];
}
