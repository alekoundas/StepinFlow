import type { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";
import type { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

/** What the recording setup asks before the first click: the flow, and the window it works in. */
export interface RecordingSetupDto {
  name: string;
  description: string;
  isSubFlow: boolean;
  isQaTest: boolean;

  // The main area, an application window.
  processName: string;
  titlePattern: string;
  titleMatchMode: TitleMatchModeEnum;
  useClientArea: boolean;
  scalesWith: ScalesWithEnum | null;

  /** Whether the flow starts by launching the application. */
  addsLaunchStep: boolean;
}
