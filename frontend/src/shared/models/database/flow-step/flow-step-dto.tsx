import type { CursorActionTypeEnum } from "@/shared/enums/backend/cursor-action-type-enum";
import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import type { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area/flow-search-area-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image/flow-step-image-dto";
import type { FlowDto } from "@/shared/models/database/flow/flow-dto";
import type { SubFlowDto } from "@/shared/models/database/sub-flow/sub-flow-dto";

import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export class FlowStepDto {
  id: number = -1;
  name: string = "";
  flowStepType: FlowStepTypeEnum = FlowStepTypeEnum.FAILURE;
  orderNumber: number = -1;

  locationX: number = 0;
  locationY: number = 0;
  locationEndX: number = 0;
  locationEndY: number = 0;

  // WAIT
  waitForMilliseconds: number = 0;

  // LOOP
  loopCount: number = 0;
  isLoopInfinite: boolean = false;

  // RUN_CMD
  runCommand: string = "";

  // VARIABLE_CONDITION
  conditionText: string = "";
  conditionType: ConditionTypeEnum = ConditionTypeEnum.NONE;

  // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
  windowName: string = "";
  windowHeight: number = 0;
  windowWidth: number = 0;

  // KYEBOARD_INPUT
  keyboardInputText: string = "";
  keyboardInputType?: KeyboardInputTypeEnum;

  // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
  isLocationCustom: boolean = false;
  isLocationEndCustom: boolean = false;
  cursorActionType?: CursorActionTypeEnum;
  cursorButtonType?: CursorButtonTypeEnum;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum;

  // Keep the root Flow or SubFlow id for easier and faster queries
  rootId: number = -1;

  // Flow
  flowId?: number;
  flow?: FlowDto;

  // Sub Flow
  subFlowId?: number;
  subFlow?: SubFlowDto;

  // FlowSearchArea
  flowSearchAreaId?: number;
  flowSearchArea?: FlowSearchAreaDto;

  // Parent FlowStep
  parentFlowStepId?: number;
  parentFlowStep?: FlowStepDto;

  // General FlowStep reference for multiple types
  flowStepReferenceId?: number;
  flowStepReference?: FlowStepDto;

  // Navigation collections
  childrenFlowSteps: FlowStepDto[] = [];
  flowStepReferences: FlowStepDto[] = [];
  flowStepImages: FlowStepImageDto[] = [];

  constructor(data: Partial<FlowStepDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
