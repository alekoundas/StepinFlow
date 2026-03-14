import { FlowSearchArea } from "./flow-search-area";
import { FlowStep } from "./flow-step";

export interface SubFlow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}
