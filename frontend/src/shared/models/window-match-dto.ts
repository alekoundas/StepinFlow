import type { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

export interface WindowMatchTestRequestDto {
  processName: string;
  titlePattern: string;
  titleMatchMode: TitleMatchModeEnum;

  /** So the reported bounds are the ones the area will actually resolve to. */
  useClientArea: boolean;
}

export interface WindowMatchDto {
  title: string;
  processName: string;

  x: number;
  y: number;
  width: number;
  height: number;
}

export interface WindowMatchTestResultDto {
  /** In z-order. The count matters: the first one is the one that runs. */
  matches: WindowMatchDto[];
}
