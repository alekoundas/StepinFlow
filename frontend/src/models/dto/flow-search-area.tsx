import { Flow } from "./flow";
import { FlowStep } from "./flow-step";

export interface IFlowSearchArea {
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

export class FlowSearchArea implements IFlowSearchArea {
  id = -1;
  name = "";

  locationLeft = -1;
  locationTop = -1;
  locationToRight = -1;
  locationToBottom = -1;

  // Flow
  flowId = -1;
  flow = new Flow();

  flowSteps: FlowStep[] = [];
}
