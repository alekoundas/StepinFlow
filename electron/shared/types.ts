export interface IpcRequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

export interface IpcResponseMessage<T> {
  action: string;
  payload: T;
  correlationId: string;
  error?: string;
}

export interface IpcBroadcastMessage<T> {
  type: string;
  payload: T;
}

export interface MonitorInfo {
  deviceId: string;
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
  physicalWidth: number;
  physicalHeight: number;
  logicalWidth: number;
  logicalHeight: number;
  scaleFactor: number;
  monitorLogicalOrigin: { x: number; y: number };
}

export interface SignalMouseEvent {
  type: "down" | "move" | "up";
  physicalX: number;
  physicalY: number;
}
// export interface ScreenshotRequestDto {
//   formatType: "JPEG" | "PNG" | "RAW";
//   jpegQuality: number;
//   captureMonitor?: string;
//   captureAppWindow?: string;
//   captureVirtualScreen?: boolean;
//   flowSearchAreaId?: number;
//   locationX?: number;
//   locationY?: number;
//   width?: number;
//   height?: number;
// }

export interface ScreenshotMonitorResponseDto {
  screenshot: Uint8Array;
  logicalX: number;
  logicalY: number;
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
