interface RequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

interface MonitorInfo {
  logicalBounds: Electron.Rectangle;
  physicalBounds: Electron.Rectangle;
  scaleFactor: number;
}
interface VirtualMonitor {
  displays: MonitorInfo[];
  minVirtualX: number;
  minVirtualY: number;
  physicalVirtualWidth: number;
  physicalVirtualHeight: number;
  logicalVirtualWidth: number;
  logicalVirtualHeight: number;
}
interface SignalReadyResponse {
  screenshot: Uint8Array;
  monitorsInfo: MonitorInfo[];
  physicalWidth: number;
  physicalHeight: number;
}

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = unknown>(msg: RequestMessage) => Promise<T>;
        onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
      };
      searchArea: {
        openWindow: (
          screenshotRequest: any,
        ) => Promise<Electron.Rectangle | null>;
        sendResultToWindow: (rect: Electron.Rectangle | null) => void;
        signalReady: () => Promise<SignalReadyResponse>;
      };
      imageEditor: {
        openWindow: (
          screenshotRequest: any,
        ) => Promise<Electron.Rectangle | null>;
        sendResultToWindow: (rect: Electron.Rectangle | null) => void;
        signalReady: () => Promise<Uint8Array | null>;
      };
    };
  }
}

export {};
