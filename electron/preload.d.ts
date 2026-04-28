interface IpcRequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

// interface MonitorInfo {
//   logicalBounds: Electron.Rectangle;
//   physicalBounds: Electron.Rectangle;
//   scaleFactor: number;
// }
// interface VirtualMonitor {
//   displays: MonitorInfo[];
//   minVirtualX: number;
//   minVirtualY: number;
//   physicalVirtualWidth: number;
//   physicalVirtualHeight: number;
//   logicalVirtualWidth: number;
//   logicalVirtualHeight: number;
// }
interface SignalReadyResponse {
  screenshot: Uint8Array;
  physicalWidth: number;
  physicalHeight: number;
  logicalWidth: number;
  logicalHeight: number;
  scaleFactor: number;
  monitorLogicalOrigin: { x: number; y: number };
}

interface SignalMouseEvent {
  type: "down" | "move" | "up";
  startPhysicalX: number;
  startPhysicalY: number;
  currentPhysicalX: number;
  currentPhysicalY: number;
}

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = unknown>(msg: IpcRequestMessage) => Promise<T>;
        onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
      };
      searchArea: {
        openWindow: () => Promise<Electron.Rectangle | null>;
        broadcastMouseEvent: (callback: (e: SignalMouseEvent) => void) => void;
        signalReady: () => Promise<SignalReadyResponse | null>;
        signalMouseEvent: (event: SignalMouseEvent) => void;
        signalCloseWindow: (rect: Electron.Rectangle | null) => void;
      };
      imageEditor: {
        openWindow: (
          screenshotRequest: any,
        ) => Promise<Electron.Rectangle | null>;
        sendResultToWindow: (rect: Electron.Rectangle | null) => void;
        signalReady: () => Promise<SignalReadyResponse>;
      };
    };
  }
}

export {};
