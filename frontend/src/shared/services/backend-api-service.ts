import type { IpcRequestMessage } from "../../../../electron/shared/types";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { SubFlowDto } from "@/shared/models/database/sub-flow-dto";
import type { LookupRequestDto } from "@/shared/models/lazy-data/lookup-request.dto";
import type { LookupResponseDto } from "@/shared/models/lazy-data/lookup-response.dto";
import type { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";

export const backendApiService = {
  greet: (name: string) => call<{ greeting: string }>("greet", { name }),

  Flow: {
    create: (dto: FlowDto) => call<number>("Flow.create", dto),
    update: (dto: FlowDto) => call<{ newId: number }>("Flow.update", dto),
    delete: (id: number) => call<{ success: boolean }>("Flow.delete", id),

    get: (id: number) => call<FlowDto>("Flow.get", id),
    getLazy: (dto: LazyDto) =>
      call<LazyResponseDto<FlowDto>>("Flow.getLazy", dto),
    getTreeNodes: (id: number) => call<TreeNodeDto[]>("Flow.getTreeNodes", id),
  },

  FlowStep: {
    create: (dto: FlowStepDto) => call<number>("FlowStep.create", dto),
    update: (dto: FlowStepDto) =>
      call<{ newId: number }>("FlowStep.update", dto),
    delete: (id: number) => call<{ success: boolean }>("FlowStep.delete", id),

    get: (id: number) => call<FlowStepDto>("FlowStep.get", id),
    getDataTable: (dto: LazyDto) =>
      call<LazyResponseDto<FlowStepDto>>("FlowStep.getLazy", dto),
    getTreeNodes: (id: number) =>
      call<TreeNodeDto[]>("FlowStep.getTreeNodes", id),
  },

  FlowStepImage: {
    create: (dto: FlowStepImageDto) =>
      call<number>("FlowStepImage.create", dto),
    update: (dto: FlowStepImageDto) =>
      call<{ newId: number }>("FlowStepImage.update", dto),
    get: (id: number) => call<FlowStepImageDto>("FlowStepImage.get", id),
  },

  FlowSearchArea: {
    create: (dto: FlowSearchAreaDto) =>
      call<number>("FlowSearchArea.create", dto),
    update: (dto: FlowSearchAreaDto) =>
      call<{ newId: number }>("FlowSearchArea.update", dto),
    get: (id: number) => call<FlowSearchAreaDto>("FlowSearchArea.get", id),
  },

  SubFlow: {
    create: (dto: SubFlowDto) => call<number>("SubFlow.create", dto),
    update: (dto: SubFlowDto) => call<{ newId: number }>("SubFlow.update", dto),
    get: (id: number) => call<SubFlowDto>("SubFlow.get", id),
  },

  Lookup: {
    window: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.window", dto),
    monitor: (dto: LookupRequestDto) =>
      call<LookupResponseDto>("Lookup.monitor", dto),
  },

  System: {
    takeScreenshot: (dto: ScreenshotRequestDto) =>
      call<Uint8Array>("System.takeScreenshot", dto),
    inputRecordOverlayStart: () =>
      call<boolean>("System.inputRecordOverlayStart"),
    inputRecordOverlayStop: () =>
      call<boolean>("System.inputRecordOverlayStop"),
  },

  OnBroadcast: (callback: (msg: any) => void): (() => void) => {
    return window.electronApi.backendApi.onBroadcast(callback);
  },
};

async function call<T = any>(action: string, payload: any = {}): Promise<T> {
  const msg: IpcRequestMessage = {
    action,
    payload,
  };

  try {
    const resultDto = await window.electronApi.backendApi.invoke<T>(msg);
    if (resultDto.isSuccess && resultDto.data) {
      return resultDto.data;
    }

    console.error(`Backend call failed [${action}]`);
    console.error(resultDto?.errorMessage);
    throw "Backend call failed [${action}]";
  } catch (err: any) {
    throw err;
  }
}

// Optional: Keep this ONLY for unsolicited messages (progress, events, etc.)
export function setupPushListener(callback: (msg: any) => void): () => void {
  return window.electronApi.backendApi.onBroadcast(callback);
}
