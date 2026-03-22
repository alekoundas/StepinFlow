import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area/flow-search-area-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";

export class SubFlowDto {
  id: number = -1;
  name: string = "";
  orderNumber: number = -1;

  flowSteps: FlowStepDto[] = [];
  flowSearchAreas: FlowSearchAreaDto[] = [];
}
