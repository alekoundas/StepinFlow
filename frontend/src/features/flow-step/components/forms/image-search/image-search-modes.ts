import { ImageSearchModeEnum } from "@/shared/enums/backend/image-search-mode-enum";

export interface ImageSearchMode {
  value: ImageSearchModeEnum;
  label: string;
  description: string;
}

export const IMAGE_SEARCH_MODES: ImageSearchMode[] = [
  {
    value: ImageSearchModeEnum.FIND_ONCE,
    label: "Find once",
    description: "Look once, then branch on what was found.",
  },
  {
    value: ImageSearchModeEnum.WAIT_UNTIL_FOUND,
    label: "Wait until found",
    description:
      "Keep checking until it appears. Replaces guessing a fixed wait, which is what breaks flows on a slower machine.",
  },
  {
    value: ImageSearchModeEnum.WAIT_UNTIL_GONE,
    label: "Wait until gone",
    description:
      "Keep checking until it disappears. For loading spinners and transition overlays.",
  },
];
