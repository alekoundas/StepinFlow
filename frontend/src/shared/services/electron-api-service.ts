import type {
  AreaRect,
  RequestMessage,
} from "../../../../electron/shared/types";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { ResultDto } from "@/shared/models/result-dto";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { SubFlowDto } from "@/shared/models/database/sub-flow-dto";
import type { LookupRequestDto } from "@/shared/models/lazy-data/lookup-request.dto";
import type { LookupResponseDto } from "@/shared/models/lazy-data/lookup-response.dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { send } from "vite";

// TODO remove this. Buut Build process throws error without it....
// const backendApi = window.backendApi; // old way
declare const electronApi: {
  backendApi: {
    invoke: <T>(msg: any) => Promise<ResultDto<T>>;
    onMessage: <T>(cb: (msg: T) => void) => () => void;
  };
  //  backendApi: {
  //   invoke: <T = unknown>(msg: RequestMessage) => Promise<T>;
  //   onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
  // };
  searchArea: {
    capture: () => Promise<AreaRect | null>;
    sendResult: (rect: AreaRect | null) => void;
    onScreenshot: (callback: (dataUrl: string) => void) => () => void;
    signalReady: () => void;
  };
};
///
//
// const x: string = 123; // ← this should instantly show error
export const ElectronApiService = {
  backendApi: backendApiService,
  searchArea: {
    capture: () => electronApi.searchArea.capture(),
    sendResult: (rect: AreaRect | null) =>
      electronApi.searchArea.sendResult(rect),
    onScreenshot: (callback: (dataUrl: string) => void) =>
      electronApi.searchArea.onScreenshot(callback),
    signalReady: () => electronApi.searchArea.signalReady(),
  },
};


// Optional: Keep this ONLY for unsolicited messages (progress, events, etc.)
export function setupPushListener(callback: (msg: any) => void): () => void {
  return electronApi.backendApi.onMessage(callback);
}
