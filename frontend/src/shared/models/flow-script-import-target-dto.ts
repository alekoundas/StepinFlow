/** What an import meets: the flow with the file's Id, and whether another flow has its name. */
export interface FlowScriptImportTargetDto {
  /** Null when the file is a new flow. */
  flowId: number | null;
  flowName: string;
  executionCount: number;

  /** Why the name cannot be used. Null when it is free. */
  nameTaken: string | null;
}
