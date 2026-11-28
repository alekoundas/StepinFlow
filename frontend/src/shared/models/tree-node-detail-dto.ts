import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import type { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import type { KeyboardKeyActionTypeEnum } from "@/shared/enums/backend/keyboard-key-action-type-enum";
import type { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import type { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import type { ShellTypeEnum } from "@/shared/enums/backend/shell/shell-type-enum";
import type { ShellCommandModeEnum } from "@/shared/enums/backend/shell/shell-command-mode-enum";
import type { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";

// Display only. What a tree row shows under the step name.
export interface TreeNodeDetailDto {
  waitForMilliseconds: number;
  waitForMillisecondsMax: number;
  loopCount: number;
  isLoopInfinite: boolean;

  areaName?: string | null;
  pointName?: string | null;
  pointEndName?: string | null;
  referenceStepName?: string | null;
  referenceStepEndName?: string | null;
  subFlowName?: string | null;

  cursorButtonType?: CursorButtonTypeEnum | null;
  cursorButtonActionType?: CursorButtonActionTypeEnum | null;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum | null;

  keyboardInputText?: string | null;
  keyboardInputType?: KeyboardInputTypeEnum | null;
  keyboardKeyActionType?: KeyboardKeyActionTypeEnum | null;

  windowWidth: number;
  windowHeight: number;
  isCloseForced: boolean;

  searchMode?: SearchModeEnum | null;
  templateCount: number;
  // Small PNG, arrives as base64.
  thumbnail?: string | null;

  conditionText?: string | null;
  conditionTextEnd?: string | null;
  conditionType?: ConditionTypeEnum | null;

  shellType?: ShellTypeEnum | null;
  shellCommandMode?: ShellCommandModeEnum | null;
  shellCommandValue?: string | null;
  shellArguments?: string | null;

  systemActionType?: SystemActionTypeEnum | null;

  message?: string | null;
  endExecutionAsSuccess: boolean;

  childCount: number;
}
