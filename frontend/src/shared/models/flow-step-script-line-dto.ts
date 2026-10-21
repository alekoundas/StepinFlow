// A step as its flow's script would print it, and the names of what it points at, which a form
// has only as ids.
export interface FlowStepScriptLineDto {
  script: string;

  // What a cursor, wait, keyboard, window or system action step is called once saved, before a
  // number makes it unique. The step's own name for the rest.
  name: string;

  areaName?: string | null;
  pointName?: string | null;
  pointEndName?: string | null;
  referenceName?: string | null;
  referenceEndName?: string | null;
  subFlowName?: string | null;
}
