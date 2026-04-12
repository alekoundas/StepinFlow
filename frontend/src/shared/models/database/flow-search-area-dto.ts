import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";

export class FlowSearchAreaDto {
  id: number = 0;
  name: string = "";
  type: FlowSearchAreaTypeEnum = "CUSTOM";

  applicationName: string = "";
  monitorName: string = "";

  // Custom search area
  locationX: number = 0;
  locationY: number = 0;
  width: number = 0;
  height: number = 0;

  // Flow
  flowId: number = 0;
  flow: FlowDto = new FlowDto();

  flowSteps: FlowStepDto[] = [];

  constructor(data: Partial<FlowSearchAreaDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
