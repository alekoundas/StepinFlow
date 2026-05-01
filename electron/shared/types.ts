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

export interface ScreenshotMonitorResponseDto {
  screenshot: Uint8Array;
  logicalX: number;
  logicalY: number;
  physicalWidth: number;
  physicalHeight: number;
}

export interface RecordedInput {
  type: RecordedInputTypeEnum;
  physicalX: number;
  physicalY: number;
  cursorButtonType: CursorButtonTypeEnum;
  createdOn: Date;
}

// Enums

export const RecordedInputTypeEnum = {
  BUTTON_UP: "BUTTON_UP",
  BUTTON_DOWN: "BUTTON_DOWN",
  CURSOR_DRAG: "CURSOR_DRAG",
  CURSOR_MOVE: "CURSOR_MOVE",
  CURSOR_SCROLL: "CURSOR_SCROLL",
} as const;
export type RecordedInputTypeEnum =
  (typeof RecordedInputTypeEnum)[keyof typeof RecordedInputTypeEnum];

export const CursorButtonTypeEnum = {
  LEFT_BUTTON: "LEFT_BUTTON",
  RIGHT_BUTTON: "RIGHT_BUTTON",
  MIDDLE_BUTTON: "MIDDLE_BUTTON",
} as const;
export type CursorButtonTypeEnum =
  (typeof CursorButtonTypeEnum)[keyof typeof CursorButtonTypeEnum];
