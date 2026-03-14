import type { Flow } from "./flow";
import type { FlowStep } from "./flow-step";

export interface FlowSearchArea {
  id: number;
  name: string;

  locationLeft: number;
  locationTop: number;
  locationToRight: number;
  locationToBottom: number;

  // Flow
  flowId: number;
  flow: Flow;

  flowSteps: FlowStep[];
}
