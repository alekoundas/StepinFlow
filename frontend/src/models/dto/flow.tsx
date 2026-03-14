import type { FlowSearchArea } from "./flow-search-area";
import type { FlowStep } from "./flow-step";

export interface Flow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}
