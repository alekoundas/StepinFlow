import type { FlowSearchAreaCreateDto } from "@/shared/models/flow-search-area/flow-search-area-create-dto";

export class SubFlowCreateDto {
  // id: number = -1;
  name: string = "";
  orderNumber: number = -1;

  // flowSteps: FlowStepCreateDto[] = [];
  flowSearchAreas: FlowSearchAreaCreateDto[] = [];
}
