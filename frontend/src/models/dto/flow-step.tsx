import { ConditionTypeEnum } from "../enums/condition-type-enum";
import { CursorActionTypeEnum } from "../enums/cursor-action-type-enum";
import { CursorButtonTypeEnum } from "../enums/cursor-button-type-enum";
import { CursorScrollDirectionTypeEnum } from "../enums/cursor-scroll-direction-type-enum";
import { FlowStepTypeEnum } from "../enums/flow-step-types-enum";
import { KeyboardInputTypeEnum } from "../enums/keyboard-input-type-enum";
import type { Flow } from "./flow";
import type { FlowSearchArea } from "./flow-search-area";
import type { FlowStepImage } from "./flow-step-image";
import type { SubFlow } from "./sub-flow";

export interface IFlowStep {
  id: number;
  name: string;
  flowStepType: FlowStepTypeEnum;
  orderNumber: number;

  locationX: number;
  locationY: number;
  locationEndX: number;
  locationEndY: number;

  // WAIT
  waitForMilliseconds: number;

  // LOOP
  loopCount: number;
  isLoopInfinite: boolean;

  // RUN_CMD
  runCommand: string;

  // VARIABLE_CONDITION
  conditionText: string;
  conditionType: ConditionTypeEnum;

  // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
  windowName: string;
  windowHeight: number;
  windowWidth: number;

  // KYEBOARD_INPUT
  keyboardInputText: string;
  keyboardInputType?: KeyboardInputTypeEnum;

  // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
  isLocationCustom: boolean;
  isLocationEndCustom: boolean;
  cursorActionType?: CursorActionTypeEnum;
  cursorButtonType?: CursorButtonTypeEnum;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum;

  // Flow
  flowId?: number;
  flow?: Flow;

  // Sub Flow
  subFlowId?: number;
  subFlow?: SubFlow;

  // FlowSearchArea
  flowSearchAreaId?: number;
  flowSearchArea?: FlowSearchArea;

  // Parent FlowStep
  parentFlowStepId?: number;
  parentFlowStep?: FlowStep;

  // General FlowStep reference for multiple types
  flowStepReferenceId?: number;
  flowStepReference?: FlowStep;

  // Navigation collections
  childrenFlowSteps: FlowStep[];
  flowStepReferences: FlowStep[];
  flowStepImages: FlowStepImage[];
}

export class FlowStep implements IFlowStep {
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

  // Flow
  flowId?: number;
  flow?: Flow;

  // Sub Flow
  subFlowId?: number;
  subFlow?: SubFlow;

  // FlowSearchArea
  flowSearchAreaId?: number;
  flowSearchArea?: FlowSearchArea;

  // Parent FlowStep
  parentFlowStepId?: number;
  parentFlowStep?: FlowStep;

  // General FlowStep reference for multiple types
  flowStepReferenceId?: number;
  flowStepReference?: FlowStep;

  // Navigation collections
  childrenFlowSteps: FlowStep[] = [];
  flowStepReferences: FlowStep[] = [];
  flowStepImages: FlowStepImage[] = [];
}
