interface AreaRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

interface RequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = unknown>(msg: RequestMessage) => Promise<T>;
        onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
      };
      searchArea: {
        openWindow: (screenshotRequest: any) => Promise<AreaRect | null>;
        sendResultToWindow: (rect: AreaRect | null) => void;
        signalReady: () => Promise<Uint8Array>;
      };
      imageEditor: {
        openWindow: (screenshotRequest: any) => Promise<AreaRect | null>;
        sendResultToWindow: (rect: AreaRect | null) => void;
        signalReady: () => Promise<Uint8Array | null>;
      };
    };
  }
}

export {};
