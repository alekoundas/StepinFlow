import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";
import type { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";

export class FlowAreaDto {
  // 0 for a new one. Areas are saved one at a time, from the editor's Areas tab.
  id: number = 0;
  name: string = "";
  type: FlowAreaTypeEnum = FlowAreaTypeEnum.CUSTOM;

  // The window the flow works in: at most one, and never inside another. A recording is stored
  // relative to it, and a viewport resizes it.
  isMain: boolean = false;

  // What makes its contents bigger or smaller on another screen. Null inherits the parent's, and
  // an area with no parent is DPI.
  scalesWith?: ScalesWithEnum | null;
  // The DPI its own offset and size were captured at. A point carries its own.
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
