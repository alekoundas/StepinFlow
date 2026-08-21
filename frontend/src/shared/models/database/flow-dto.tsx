import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

export class FlowDto {
  id: number = 0;
  name: string = "";
  orderNumber: number = -1;

  /** A flow meant to be called by another rather than started on its own. One way, never unset. */
  isSubFlow: boolean = false;

  flowSteps: FlowStepDto[] = [];
  flowAreas: FlowAreaDto[] = [];
  flowPoints: FlowPointDto[] = [];

  constructor(data: Partial<FlowDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
