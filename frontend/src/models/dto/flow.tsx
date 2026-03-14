import { FlowSearchArea } from "./flow-search-area";
import { FlowStep } from "./flow-step";

export interface Flow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}
