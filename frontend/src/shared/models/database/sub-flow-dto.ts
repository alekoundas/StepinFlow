import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

export class SubFlowDto {
  id: number = -1;
  name: string = "";
  orderNumber: number = -1;

  flowSteps: FlowStepDto[] = [];
  flowAreas: FlowAreaDto[] = [];
}
