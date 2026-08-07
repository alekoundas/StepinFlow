import type { IpcBroadcastMessage, IpcRequestMessage } from "../../../../electron/shared/types";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { TreeNodeRequestDto } from "@/shared/models/tree-node-request.dto";
import type {
  FlowStepMoveDto,
  FlowStepMovePreviewDto,
} from "@/shared/models/flow-step-move.dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
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
  },

  FlowStepImage: {
    create: (dto: FlowStepImageDto) =>
      call<number>("FlowStepImage.create", dto),
    get: (id: number) => call<FlowStepImageDto>("FlowStepImage.get", id),
  },

  FlowSearchArea: {
    create: (dto: FlowSearchAreaDto) =>
      call<number>("FlowSearchArea.create", dto),
    update: (dto: FlowSearchAreaDto) =>
      call<FlowSearchAreaDto>("FlowSearchArea.update", dto),
    delete: (id: number) => call<boolean>("FlowSearchArea.delete", id),
    get: (id: number) => call<FlowSearchAreaDto>("FlowSearchArea.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowSearchAreaDto>>("FlowSearchArea.getLazy", dto),
  },

  FlowLocation: {
    create: (dto: FlowLocationDto) => call<number>("FlowLocation.create", dto),
    update: (dto: FlowLocationDto) =>
      call<FlowLocationDto>("FlowLocation.update", dto),
    delete: (id: number) => call<boolean>("FlowLocation.delete", id),
    get: (id: number) => call<FlowLocationDto>("FlowLocation.get", id),
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
    flowLocation: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.flowLocation", dto),
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
