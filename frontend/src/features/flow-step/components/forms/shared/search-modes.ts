import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";

export interface SearchMode {
  value: SearchModeEnum;
  label: string;
  description: string;
}

export const IMAGE_SEARCH_MODES: SearchMode[] = [
  {
    value: SearchModeEnum.FIND_BEST,
    label: "Best match",
    description: "Look once and act on the strongest match, then branch on what was found.",
  },
  {
    value: SearchModeEnum.FIND_ALL,
    label: "Every match",
    description:
      "Look once and run the Success steps for each match. For acting on a whole list at once.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_FOUND,
    label: "Until found",
    description:
      "Keep checking until it appears. Replaces guessing a fixed wait, which is what breaks flows on a slower machine.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_NOT_FOUND,
    label: "Until not found",
    description:
      "Keep checking until it stops matching. For loading spinners and transition overlays.",
  },
];

/**
 * Reading an area produces one block of text and no positions, so there is nothing for
 * FIND_ALL to act on each of.
 */
export const READ_TEXT_MODES: SearchMode[] = [
  {
    value: SearchModeEnum.FIND_BEST,
    label: "Read once",
    description: "Read the area once and branch on whether it matches.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_FOUND,
    label: "Until it matches",
    description:
      "Keep reading until the text matches. Replaces guessing a fixed wait, which is what breaks flows on a slower machine.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_NOT_FOUND,
    label: "Until it stops matching",
    description: "Keep reading until the text is no longer there. For spinners and status messages.",
  },
];

export const READ_TEXT_MODE_VALUES = READ_TEXT_MODES.map((x) => x.value) as [
  SearchModeEnum,
  ...SearchModeEnum[],
];

/** The two polling modes, which are the ones that need an interval and a timeout. */
export const isWaitingMode = (mode: SearchModeEnum): boolean =>
  mode === SearchModeEnum.WAIT_UNTIL_FOUND ||
  mode === SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
