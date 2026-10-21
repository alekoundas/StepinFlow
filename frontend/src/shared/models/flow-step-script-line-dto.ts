// A step as its flow's script would print it, and the names of what it points at, which a form
// has only as ids.
export interface FlowStepScriptLineDto {
  script: string;

  areaName?: string | null;
  pointName?: string | null;
  pointEndName?: string | null;
  referenceName?: string | null;
  referenceEndName?: string | null;
  subFlowName?: string | null;
}
