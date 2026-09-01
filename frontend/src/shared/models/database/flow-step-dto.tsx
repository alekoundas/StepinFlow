import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import type { KeyboardInputTypeEnum } from "@/shared/enums/backend/keyboard-input-type-enum";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import type { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";

import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

import type { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";
import type { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import type { ResultSourceEnum } from "@/shared/enums/backend/command/result-source-enum";
import type { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";

export class FlowStepDto {
  // Core fields
  id: number = 0;
  name: string = "";
  flowStepType: FlowStepTypeEnum = FlowStepTypeEnum.FAILURE;
  orderNumber: number = -1;

  // WAIT
  waitForMilliseconds: number = 0;

  /** Upper bound of the random range. 0 waits exactly waitForMilliseconds. */
  waitForMillisecondsMax: number = 0;

  // LOOP, CURSOR_SCROLL (scroll notch count)
  loopCount: number = 0;
  isLoopInfinite: boolean = false;

  // IMAGE_SEARCH, READ_TEXT
  searchMode: SearchModeEnum = "FIND_BEST";
  templateMatchMode: TemplateMatchModeEnum = "CCoeffNormed";
  accuracy: number = 0.8;
  maxMatches: number = 20;
  pollIntervalMilliseconds: number = 500;
  timeoutMilliseconds: number = 0;

  // SYSTEM_COMMAND
  runCommandShell: RunCommandShellEnum = "CMD";
  runCommandPreset: RunCommandPresetEnum = "CUSTOM";
  // The preset's single parameter, or the whole command when the preset is CUSTOM.
  runCommandValue: string = "";
  runCommandWorkingDirectory: string = "";
  successExitCodes: string = "0";
  resultSource: ResultSourceEnum = "STDOUT";

  // SYSTEM_ACTION
  systemActionType: SystemActionTypeEnum = "LOCK_WORKSTATION";

  // SYSTEM_COMMAND, READ_TEXT
  resultExtractPattern: string = "";

  // READ_TEXT
  ocrLanguage: string = "";

  // CHECK_VALUE, READ_TEXT (the text being looked for)
  conditionText: string = "";
  conditionTextEnd: string = "";
  conditionType?: ConditionTypeEnum;

  // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
  //
  // The window is named here rather than through an APPLICATION area. Either half identifies it
  // and both together narrow. Resize and relocate always mean the outer frame.
  processName: string = "";
  titlePattern: string = "";
  titleMatchMode: TitleMatchModeEnum = TitleMatchModeEnum.CONTAINS;

  windowHeight: number = 0;
  windowWidth: number = 0;

  // KYEBOARD_INPUT
  keyboardInputText: string = "";
  keyboardInputType?: KeyboardInputTypeEnum;

  // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
  //
  // Point source per point:
  //   flowPointId         -> a reusable named point on the Flow
  //   flowStepReferenceId -> the result of an ancestor IMAGE_SEARCH / READ_TEXT
  // Never both: whichever is set is the source.
  cursorButtonType?: CursorButtonTypeEnum;
  cursorButtonActionType?: CursorButtonActionTypeEnum;
  cursorScrollDirectionType?: CursorScrollDirectionTypeEnum;

  // Keep the root Flow or SubFlow id for easier and faster queries
  rootId: number = 0;

  // Flow
  flowId?: number;
  flow?: FlowDto;

  // SUB_FLOW: the flow this step runs.
  subFlowId?: number;

  // NOTIFY
  discordBotId?: number;
  notifyMessage: string = "";
  subFlow?: FlowDto;

  // FlowArea
  flowAreaId?: number;
  flowArea?: FlowAreaDto;

  // FlowPoint (start / end point)
  flowPointId?: number;
  flowPoint?: FlowPointDto;

  flowPointEndId?: number;
  flowPointEnd?: FlowPointDto;

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
