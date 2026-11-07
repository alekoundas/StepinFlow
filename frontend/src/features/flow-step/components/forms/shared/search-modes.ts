import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";

export interface SearchMode {
  value: SearchModeEnum;
  label: string;
  description: string;
}

export const SEARCH_IMAGE_MODES: SearchMode[] = [
  {
    value: SearchModeEnum.FIND_BEST,
    label: "Best match",
    description:
      "Look once and act on the strongest match. The one to use for a branch point - \"which layout am I in\" has a final answer, so waiting on it only costs time.",
  },
  {
    value: SearchModeEnum.FIND_ALL,
    label: "Every match",
    description:
      "Look once and run the Success steps for each match. For acting on a whole list at once.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_FOUND,
    label: "Wait until found",
    description:
      "Keep checking until it appears. Replaces guessing a fixed wait, which is what breaks flows on a slower machine.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_NOT_FOUND,
    label: "Wait until not found",
    description:
      "Keep checking until it stops matching. For loading spinners and transition overlays.",
  },
];

/** Not a button of its own: the form picks it with a checkbox under "Wait until found". */
export const WAIT_UNTIL_STILL_MODE: SearchMode = {
  value: SearchModeEnum.WAIT_UNTIL_STILL,
  label: "Wait until still",
  description:
    "Keep checking until it appears and stops moving, so a click does not land where an animation was passing through.",
};

export const searchImageModeOf = (mode: SearchModeEnum): SearchMode | undefined =>
  [...SEARCH_IMAGE_MODES, WAIT_UNTIL_STILL_MODE].find((x) => x.value === mode);

/**
 * Reading an area produces one block of text and no positions, so there is nothing for
 * FIND_ALL to act on each of, and nothing to stand still.
 */
export const SEARCH_TEXT_MODES: SearchMode[] = [
  {
    value: SearchModeEnum.FIND_BEST,
    label: "Check once",
    description: "Read the area once and decide. Succeeds when the condition holds.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_FOUND,
    label: "Wait until it matches",
    description:
      "Keep reading until the condition holds. Replaces guessing a fixed wait, which is what breaks flows on a slower machine.",
  },
  {
    value: SearchModeEnum.WAIT_UNTIL_NOT_FOUND,
    label: "Wait until it stops matching",
    description: "Keep reading until the condition stops holding. For spinners and status messages.",
  },
];

export const SEARCH_TEXT_MODE_VALUES = SEARCH_TEXT_MODES.map((x) => x.value) as [
  SearchModeEnum,
  ...SearchModeEnum[],
];

/** The polling modes, which are the ones that need an interval and a timeout. */
export const isWaitingMode = (mode: SearchModeEnum): boolean =>
  mode === SearchModeEnum.WAIT_UNTIL_FOUND ||
  mode === SearchModeEnum.WAIT_UNTIL_STILL ||
  mode === SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
