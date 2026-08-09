import { AnchorTypeEnum } from "@/shared/enums/backend/area/anchor-type-enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

export class FlowLocationDto {
  id: number = 0;
  name: string = "";

  // Frame this point lives in. Null = absolute screen, which needs rebinding after an import.
  flowSearchAreaId?: number | null;
  anchor: AnchorTypeEnum = AnchorTypeEnum.TOP_LEFT;
  offsetMode: AreaSizingModeEnum = AreaSizingModeEnum.ABSOLUTE_PX;

  locationX: number = 0;
  locationY: number = 0;

  ratioX: number = 0;
  ratioY: number = 0;

  flowId: number = 0;

  // Read only, set by the backend.
  flowStepsCount: number = 0;
  flowSearchAreaName: string = "";

  constructor(data: Partial<FlowLocationDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
