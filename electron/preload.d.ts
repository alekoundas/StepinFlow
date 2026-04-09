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

// interface ElectronApi {
//   backendApi: {
//     invoke: <T = unknown>(msg: RequestMessage) => Promise<T>;
//     onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
//   };
//   searchArea: {
//     capture: () => Promise<AreaRect | null>;
//     sendResult: (rect: AreaRect | null) => void;
//     onScreenshot: (callback: (dataUrl: string) => void) => () => void;
//     signalReady: () => void;
//   };
//   imageEditor: {
//     open: (initialDataUrl: string, stepId?: string) => Promise<AreaRect>;
//   };
// }

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = unknown>(msg: RequestMessage) => Promise<T>;
        onMessage: <T = unknown>(callback: (msg: T) => void) => () => void;
      };
      searchArea: {
        capture: () => Promise<AreaRect | null>;
        sendResult: (rect: AreaRect | null) => void;
        onScreenshot: (callback: (dataUrl: string) => void) => () => void;
        signalReady: () => void;
      };
      imageEditor: {
        open: (initialDataUrl: string, stepId?: string) => Promise<AreaRect>;
      };
    };
  }
}

export {};
