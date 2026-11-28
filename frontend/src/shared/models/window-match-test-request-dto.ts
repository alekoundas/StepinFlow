import type { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

export interface WindowMatchTestRequestDto {
  processName: string;
  titlePattern: string;
  titleMatchMode: TitleMatchModeEnum;

  /** So the reported bounds are the ones the area will actually resolve to. */
  ignoreWindowBorders: boolean;

  /** Brings the first match to the front, so the tester sees which window it is. */
  bringsToFront?: boolean;
}
