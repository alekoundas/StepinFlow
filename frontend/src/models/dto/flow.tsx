import type { FlowSearchArea } from "./flow-search-area";
import type { FlowStep } from "./flow-step";

export interface IFlow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}

export class Flow implements Flow {
  id = -1;
  name = "";
  orderNumber = -1;

  flowSteps: FlowStep[] = [];
  flowSearchAreas: FlowSearchArea[] = [];
  
  constructor(data: Partial<IFlow> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
