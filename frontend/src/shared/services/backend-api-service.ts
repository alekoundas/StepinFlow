import type { IpcBroadcastMessage, IpcRequestMessage } from "../../../../electron/shared/types";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowValidationResultDto } from "@/shared/models/database/flow-validation-result-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { TreeNodeRequestDto } from "@/shared/models/tree-node-request.dto";
import type {
  FlowStepMoveDto,
  FlowStepMovePreviewDto,
} from "@/shared/models/flow-step-move.dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowAreaPreviewDto } from "@/shared/models/database/flow-area-preview-dto";
import type { ImageSearchTestResultDto } from "@/shared/models/database/image-search-test-result-dto";
import type { RunCommandTestResultDto } from "@/shared/models/database/run-command-test-result-dto";
import type { TextSearchTestResultDto } from "@/shared/models/database/text-search-test-result-dto";
import type { CommandPresetDto } from "@/shared/models/database/command-preset-dto";
import type { SubFlowDto } from "@/shared/models/database/sub-flow-dto";
import type { LookupRequestDto } from "@/shared/models/lazy-data/lookup-request.dto";
import type { LookupResponseDto } from "@/shared/models/lazy-data/lookup-response.dto";
import type { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import type { ScreenPointDto } from "@/shared/models/screen-point.dto";

export const backendApiService = {
  greet: (name: string) => call<{ greeting: string }>("greet", { name }),

  Flow: {
    create: (dto: FlowDto) => call<number>("Flow.create", dto),
    update: (dto: FlowDto) => call<FlowDto>("Flow.update", dto),
    delete: (id: number) => call<boolean>("Flow.delete", id),

    get: (id: number) => call<FlowDto>("Flow.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowDto>>("Flow.getLazy", dto),
    getTreeNodes: (id: number) => call<TreeNodeDto[]>("Flow.getTreeNodes", id),
    validate: (id: number) =>
      call<FlowValidationResultDto>("Flow.validate", id),
  },

  FlowStep: {
    create: (dto: FlowStepDto) => call<number>("FlowStep.create", dto),
    update: (dto: FlowStepDto) => call<FlowStepDto>("FlowStep.update", dto),
    delete: (id: number) => call<boolean>("FlowStep.delete", id),

    get: (id: number) => call<FlowStepDto>("FlowStep.get", id),
    getDataTable: (dto: LazyDto) =>
      call<LazyResponseDto<FlowStepDto>>("FlowStep.getLazy", dto),
    getTreeNodes: (dto: TreeNodeRequestDto) =>
      call<TreeNodeDto[]>("FlowStep.getTreeNodes", dto),
    getMovePreview: (dto: FlowStepMoveDto) =>
      call<FlowStepMovePreviewDto>("FlowStep.getMovePreview", dto),
    move: (dto: FlowStepMoveDto) => call<boolean>("FlowStep.move", dto),
    testImageSearch: (dto: FlowStepDto) =>
      call<ImageSearchTestResultDto>("FlowStep.testImageSearch", dto),
    testRunCommand: (dto: FlowStepDto) =>
      call<RunCommandTestResultDto>("FlowStep.testRunCommand", dto),
    testTextSearch: (dto: FlowStepDto) =>
      call<TextSearchTestResultDto>("FlowStep.testTextSearch", dto),
  },

  FlowStepImage: {
    create: (dto: FlowStepImageDto) =>
      call<number>("FlowStepImage.create", dto),
    get: (id: number) => call<FlowStepImageDto>("FlowStepImage.get", id),
  },

  FlowArea: {
    create: (dto: FlowAreaDto) =>
      call<number>("FlowArea.create", dto),
    update: (dto: FlowAreaDto) =>
      call<FlowAreaDto>("FlowArea.update", dto),
    delete: (id: number) => call<boolean>("FlowArea.delete", id),
    get: (id: number) => call<FlowAreaDto>("FlowArea.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowAreaDto>>("FlowArea.getLazy", dto),
    getPreview: (id: number) =>
      call<FlowAreaPreviewDto>("FlowArea.getPreview", id),
  },

  FlowPoint: {
    create: (dto: FlowPointDto) => call<number>("FlowPoint.create", dto),
    update: (dto: FlowPointDto) =>
      call<FlowPointDto>("FlowPoint.update", dto),
    delete: (id: number) => call<boolean>("FlowPoint.delete", id),
    get: (id: number) => call<FlowPointDto>("FlowPoint.get", id),
    getPreview: (id: number) =>
      call<ScreenPointDto>("FlowPoint.getPreview", id),
  },

  SubFlow: {
    create: (dto: SubFlowDto) => call<number>("SubFlow.create", dto),
    update: (dto: SubFlowDto) => call<SubFlowDto>("SubFlow.update", dto),
    delete: (id: number) => call<boolean>("SubFlow.delete", id),
    get: (id: number) => call<SubFlowDto>("SubFlow.get", id),
  },

  Lookup: {
    window: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.window", dto),
    monitor: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.monitor", dto),
    flowStep: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowStep", dto),
    flowPoint: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowPoint", dto),
    flowArea: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowArea", dto),
    commandPresets: () => call<CommandPresetDto[]>("Lookup.commandPresets"),
  },

  System: {
    // .Net returns byte[], which arrives here as a base64 string
    takeScreenshot: (dto: ScreenshotRequestDto) =>
      call<string>("System.takeScreenshot", dto),
    moveCursor: (dto: ScreenPointDto) =>
      call<boolean>("System.moveCursor", dto),
    inputRecordOverlayStart: () =>
      call<boolean>("System.inputRecordOverlayStart"),
    inputRecordOverlayStop: () =>
      call<boolean>("System.inputRecordOverlayStop"),
    inputRecordPointCaptureStart: () =>
      call<boolean>("System.inputRecordPointCaptureStart"),
    inputRecordPointCaptureStop: () =>
      call<boolean>("System.inputRecordPointCaptureStop"),
  },

  OnBroadcast: (callback: (msg: IpcBroadcastMessage<any>) => void): (() => void) => {
    return window.electronApi.backendApi.onBroadcast(callback);
  },
};

async function call<T = any>(action: string, payload: any = {}): Promise<T> {
  const msg: IpcRequestMessage = {
    action,
    payload,
  };

  const resultDto = await window.electronApi.backendApi.invoke<T>(msg);

  // Only null/undefined mean "no payload". A legitimate 0, false or "" is a value.
  if (!resultDto.isSuccess || resultDto.data === undefined || resultDto.data === null) {
    console.error(`Backend call failed [${action}]`, resultDto?.errorMessage);
    throw new Error(resultDto?.errorMessage ?? `Backend call failed [${action}]`);
  }

  return resultDto.data;
}
