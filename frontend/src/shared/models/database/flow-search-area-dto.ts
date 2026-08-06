import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";

export class FlowSearchAreaDto {
  id: number = 0;
  name: string = "";
  type: FlowSearchAreaTypeEnum = "CUSTOM";

  appWindowName: string = "";
  monitorUniqueId: string = "";

  // Custom search area
  locationX: number = 0;
  locationY: number = 0;
  width: number = 0;
  height: number = 0;

  // Flow
  flowId: number = 0;

  // How many FlowSteps use this search area. Read only, set by the backend.
  flowStepsCount: number = 0;

  constructor(data: Partial<FlowSearchAreaDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
