export interface RequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

export interface ResponseMessage<T> {
  action: string;
  payload: T;
  correlationId: string;
  error?: string;
}

export interface MonitorInfo {
  logicalBounds: Electron.Rectangle;
  physicalBounds: Electron.Rectangle;
  scaleFactor: number;
}
export interface SystemMonitor {
  displays: MonitorInfo[];
  minVirtualX: number;
  minVirtualY: number;
  physicalVirtualWidth: number;
  physicalVirtualHeight: number;
  logicalVirtualWidth: number;
  logicalVirtualHeight: number;
}
export interface SignalReadyResponse {
  screenshot: Uint8Array;
  monitorsInfo: MonitorInfo[];
  physicalWidth: number;
  physicalHeight: number;
}

// export type KnownActions = "greet" | "test" | "load-flow" | "save-config";

// export interface RequestPayloads {
//   greet: { name: string };
//   test: Record<string, never>; // empty object
// }

// export interface ResponsePayloads {
//   greet: { greeting: string };
// }
