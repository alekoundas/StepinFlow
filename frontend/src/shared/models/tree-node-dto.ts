import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import type { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import type { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import type { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";
import type { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import type { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";

// Display only. What a tree row shows under the step name.
export interface TreeNodeDetailDto {
  waitForMilliseconds: number;
  loopCount: number;
  isLoopInfinite: boolean;

  areaName?: string | null;
  pointName?: string | null;
  pointEndName?: string | null;
  referenceStepName?: string | null;
  referenceStepEndName?: string | null;
  subFlowName?: string | null;

  isPointCustom: boolean;
  isPointEndCustom: boolean;

  cursorButtonType?: CursorButtonTypeEnum | null;
  cursorButtonActionType?: CursorButtonActionTypeEnum | null;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum | null;

  keyboardInputText?: string | null;
  keyboardInputType?: KeyboardInputTypeEnum | null;

  windowWidth: number;
  windowHeight: number;

  searchMode?: SearchModeEnum | null;
  templateCount: number;
  // Small PNG, arrives as base64.
  thumbnail?: string | null;

  conditionText?: string | null;
  conditionType?: ConditionTypeEnum | null;

  runCommandShell?: RunCommandShellEnum | null;
  runCommandPreset?: RunCommandPresetEnum | null;
  runCommand?: string | null;

  systemActionType?: SystemActionTypeEnum | null;

  childCount: number;
}

/**
 * Tree key for an entity id. Must stay in sync with TreeNodeDto.BuildKey on the backend.
 *
 * Flow ids and FlowStep ids are separate sequences, so a raw id would make Flow 5 and FlowStep 5
 * the same node as far as PrimeReact selection and expansion are concerned.
 */
export const buildTreeNodeKey = (id: number, isFlow: boolean): string =>
  isFlow ? `flow-${id}` : `step-${id}`;

export class TreeNodeDto {
  key: string = "-1";
  entityId: number = 0;
  droppable: boolean = false;
  draggable: boolean = false;
  selectable: boolean = true;
  leaf: boolean = false; //Specifies if the node has children. // True doesnt allow expand
  //   className?: string;

  // Custom props
  name: string = "";
  flowStepType?: FlowStepTypeEnum;
  orderNumber: number = -1;
  isFlow: boolean = false;
  isNew: boolean = true;

  parentFlowId?: number | null;
  parentFlowStepId?: number | null;

  /** What the row shows besides the name. Null for a Flow node. */
  detail?: TreeNodeDetailDto | null;

  children: TreeNodeDto[] = [];

  constructor(data: Partial<TreeNodeDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
