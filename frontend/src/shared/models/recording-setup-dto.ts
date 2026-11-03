import type { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";
import type { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import type { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";

/** What the recording setup asks before the first click: the flow, and the window it works in. */
export interface RecordingSetupDto {
  name: string;
  description: string;
  isSubFlow: boolean;
  isQaTest: boolean;

  // The main area: an application's window, or a website's tab in a browser.
  areaType: FlowAreaTypeEnum;
  processName: string;
  titlePattern: string;
  titleMatchMode: TitleMatchModeEnum;
  tabAddress: string;
  useClientArea: boolean;
  scalesWith: ScalesWithEnum | null;

  /** Whether the flow starts by launching the application. */
  addsLaunchStep: boolean;

  /** Whether the flow then sets its window to windowWidth by windowHeight, title bar and borders included. */
  addsResizeStep: boolean;
  windowWidth: number;
  windowHeight: number;
}
