import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";
import type { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";

export class FlowAreaDto {
  // 0 for a new row. New rows get a negative id so a sibling added in the same save can
  // reference them; the backend swaps them for real ids after the insert.
  id: number = 0;
  name: string = "";
  type: FlowAreaTypeEnum = FlowAreaTypeEnum.CUSTOM;

  // What makes its contents bigger or smaller on another screen. Null inherits the parent's, and
  // an area with no parent is DPI.
  scalesWith?: ScalesWithEnum | null;
  // The DPI its pixel numbers were written at.
  authoredDpi: number = 0;

  // CUSTOM
  parentFlowAreaId?: number | null;
  sizingMode: AreaSizingModeEnum = AreaSizingModeEnum.ABSOLUTE_PX;

  locationX: number = 0;
  locationY: number = 0;
  width: number = 0;
  height: number = 0;

  ratioX: number = 0;
  ratioY: number = 0;
  ratioWidth: number = 0;
  ratioHeight: number = 0;

  // APPLICATION, BROWSER_TAB
  processName: string = "";
  titlePattern: string = "";
  titleMatchMode: TitleMatchModeEnum = TitleMatchModeEnum.CONTAINS;
  useClientArea: boolean = true;

  // BROWSER_TAB
  tabMatchValue: string = "";
  tabMatchOn: TabMatchOnEnum = TabMatchOnEnum.TITLE;

  // MONITOR. Empty is the primary monitor.
  monitorDeviceName: string = "";

  flowId: number = 0;

  // Read only, set by the backend.
  flowStepsCount: number = 0;
  parentName: string = "";

  constructor(data: Partial<FlowAreaDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
