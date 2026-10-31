/** The Check page dropping a check a model added: the flow, the check, and the ones still undecided. */
export interface ProposedCheckDropDto {
  flowId: number;
  name: string;

  /** The proposed checks not yet kept or dropped, by name, so the answer still offers them. */
  undecided: string[];
}
