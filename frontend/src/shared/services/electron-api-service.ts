import type { AreaRect } from "../../../../electron/shared/types";
import type { ResultDto } from "@/shared/models/result-dto";
import { backendApiService } from "@/shared/services/backend-api-service";

// TODO remove this. Buut Build process throws error without it....
// const backendApi = window.backendApi; // old way
declare const electronApi: {
  backendApi: {
    invoke: <T>(msg: any) => Promise<ResultDto<T>>;
    onMessage: <T>(cb: (msg: T) => void) => () => void;
  };
  searchArea: {
    capture: () => Promise<AreaRect | null>;
    sendResult: (rect: AreaRect | null) => void;
    onScreenshot: (callback: (dataUrl: string) => void) => () => void;
    signalReady: () => void;
  };
};

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
