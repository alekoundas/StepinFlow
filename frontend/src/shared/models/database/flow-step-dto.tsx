import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import type { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { SubFlowDto } from "@/shared/models/database/sub-flow-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";

import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

export class FlowStepDto {
  // Core fields
  id: number = 0;
  name: string = "";
  flowStepType: FlowStepTypeEnum = FlowStepTypeEnum.FAILURE;
  orderNumber: number = -1;

  // WINDOW_RELOCATE, WINDOW_RESIZE
  locationX: number = 0;
  locationY: number = 0;
  locationEndX: number = 0;
  locationEndY: number = 0;

  // WAIT
  waitForMilliseconds: number = 0;

  // LOOP, CURSOR_SCROLL (scroll notch count)
  loopCount: number = 0;
  isLoopInfinite: boolean = false;

  // RUN_CMD
  runCommand: string = "";

  // VARIABLE_CONDITION
  conditionText: string = "";
  conditionType?: ConditionTypeEnum;

  // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
  windowName: string = "";
  windowHeight: number = 0;
  windowWidth: number = 0;

  // KYEBOARD_INPUT
  keyboardInputText: string = "";
  keyboardInputType?: KeyboardInputTypeEnum;

  // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
  //
  // Point source per point:
  //   isLocationCustom = true  -> flowLocationId       (reusable named point on the Flow)
  //   isLocationCustom = false -> flowStepReferenceId  (result of an ancestor IMAGE_SEARCH / TEXT_SEARCH)
  isLocationCustom: boolean = false;
  isLocationEndCustom: boolean = false;
  cursorButtonType?: CursorButtonTypeEnum;
  cursorButtonActionType?: CursorButtonActionTypeEnum;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum;

  // Keep the root Flow or SubFlow id for easier and faster queries
  rootId: number = 0;

  // Flow
  flowId?: number;
  flow?: FlowDto;

  // Sub Flow
  subFlowId?: number;
  subFlow?: SubFlowDto;

  // FlowSearchArea
  flowSearchAreaId?: number;
  flowSearchArea?: FlowSearchAreaDto;

  // FlowLocation (start / end point)
  flowLocationId?: number;
  flowLocation?: FlowLocationDto;

  flowLocationEndId?: number;
  flowLocationEnd?: FlowLocationDto;

  // Parent FlowStep
  parentFlowStepId?: number;
  parentFlowStep?: FlowStepDto;

  // General FlowStep reference for multiple types (start / end point)
  flowStepReferenceId?: number;
  flowStepReference?: FlowStepDto;

  flowStepReferenceEndId?: number;
  flowStepReferenceEnd?: FlowStepDto;

  // Navigation collections
  childrenFlowSteps: FlowStepDto[] = [];
  flowStepReferences: FlowStepDto[] = [];
  flowStepReferencesEnd: FlowStepDto[] = [];
  flowStepImages: FlowStepImageDto[] = [];

  constructor(data: Partial<FlowStepDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
