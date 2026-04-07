import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";

export class FlowSearchAreaDto {
  id: number = -1;
  name: string = "";

  flowSearchAreaType: FlowSearchAreaTypeEnum = "CUSTOM";

  applicationName: string = "";
  monitorIndex: string = "";

  // Custom search area
  locationX: number = -1;
  locationY: number = -1;
  width: number = -1;
  height: number = -1;

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
