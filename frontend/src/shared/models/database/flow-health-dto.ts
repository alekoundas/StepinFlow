/** Counts only. The messages belong on the flow, not in a list. */
export interface FlowHealthDto {
  flowId: number;
  flowName: string;
  isSubFlow: boolean;
  errorCount: number;
  warningCount: number;
}
