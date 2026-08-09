import { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { BrowserTypeEnum } from "@/shared/enums/backend/area/browser-type-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";

export class FlowSearchAreaDto {
  // 0 for a new row. New rows get a negative id so a sibling added in the same save can
  // reference them; the backend swaps them for real ids after the insert.
  id: number = 0;
  name: string = "";
  type: FlowSearchAreaTypeEnum = FlowSearchAreaTypeEnum.CUSTOM;

  // CUSTOM
  parentFlowSearchAreaId?: number | null;
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
  instanceIndex: number = 0;
  useClientArea: boolean = true;

  // BROWSER_TAB
  browserType: BrowserTypeEnum = BrowserTypeEnum.ANY;
  tabMatchValue: string = "";
  tabMatchOn: TabMatchOnEnum = TabMatchOnEnum.TITLE;

  // MONITOR
  monitorUniqueId: string = "";

  flowId: number = 0;

  // Read only, set by the backend.
  flowStepsCount: number = 0;
  parentName: string = "";

  constructor(data: Partial<FlowSearchAreaDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
