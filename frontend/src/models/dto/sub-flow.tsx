import type { FlowSearchArea } from "./flow-search-area";
import type { FlowStep } from "./flow-step";

export interface SubFlow {
  id: number;
  name: string;
  orderNumber: number;

  flowSteps: FlowStep[];
  flowSearchAreas: FlowSearchArea[];
}
