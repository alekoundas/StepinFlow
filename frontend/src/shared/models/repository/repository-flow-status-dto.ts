/** A flow with changes not committed yet. */
export interface RepositoryFlowStatusDto {
  flowId: number;
  flowName: string;
  changeCount: number;
}
