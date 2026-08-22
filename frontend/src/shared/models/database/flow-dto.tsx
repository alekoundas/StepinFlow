import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

export class FlowDto {
  id: number = 0;
  name: string = "";

  /** One line saying what it does. The only field that makes a list of flows readable. */
  description: string = "";

  /** A flow meant to be called by another rather than started on its own. One way, never unset. */
  isSubFlow: boolean = false;

  createdOn?: string;
  updatedOn?: string | null;

  // Projected for the list. Zero on anything the form loaded.
  stepCount: number = 0;
  areaCount: number = 0;
  pointCount: number = 0;

  /** Sub-flows only: how many distinct flows invoke this one. */
  callerCount: number = 0;

  flowSteps: FlowStepDto[] = [];
  flowAreas: FlowAreaDto[] = [];
  flowPoints: FlowPointDto[] = [];

  constructor(data: Partial<FlowDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
