import type { FlowStepDto } from "@/shared/models/flow-step/flow-step-dto";
import { FlowDto } from "@/shared/models/flow/flow-dto";

export class FlowSearchAreaDto {
  id: number = -1;
  name: string = "";

  locationLeft: number = -1;
  locationTop: number = -1;
  locationToRight: number = -1;
  locationToBottom: number = -1;

  // Flow
  flowId: number = -1;
  flow: FlowDto = new FlowDto();

  flowSteps: FlowStepDto[] = [];

  constructor(data: Partial<FlowSearchAreaDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
