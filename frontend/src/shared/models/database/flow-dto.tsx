import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

export class FlowDto {
  id: number = 0;
  name: string = "";
  orderNumber: number = -1;

  flowSteps: FlowStepDto[] = [];
  flowSearchAreas: FlowSearchAreaDto[] = [];
  flowLocations: FlowLocationDto[] = [];

  constructor(data: Partial<FlowDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
