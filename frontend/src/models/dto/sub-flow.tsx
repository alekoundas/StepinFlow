import type { FlowSearchArea } from "./flow-search-area";
import type { FlowStep } from "./flow-step";

export interface ISubFlow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}

export class SubFlow implements ISubFlow {
  id = -1;
  name = "";
  orderNumber = -1;

  flowSteps: FlowStep[] = [];
  flowSearchAreas: FlowSearchArea[] = [];
}
