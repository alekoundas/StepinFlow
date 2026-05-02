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

  // Cursor
  physicalX: number;
  physicalY: number;
  cursorButtonType: CursorButtonTypeEnum;

  // Keyboard
  keyCode?: KeyCodeEnum;
  KeyChar?: string; // Human readable

  createdOn: Date;
}

// Enums

export const RecordedInputTypeEnum = {
  // Cursor
  BUTTON_UP: "BUTTON_UP",
  BUTTON_DOWN: "BUTTON_DOWN",
  CURSOR_DRAG: "CURSOR_DRAG",
  CURSOR_MOVE: "CURSOR_MOVE",
  CURSOR_SCROLL: "CURSOR_SCROLL",
  // Keyboard
  KEY_UP: "KEY_UP",
  KEY_DOWN: "KEY_DOWN",
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

export const KeyCodeEnum = {
  // Letters
  A: "A",
  B: "B",
  C: "C",
  D: "D",
  E: "E",
  F: "F",
  G: "G",
  H: "H",
  I: "I",
  J: "J",
  K: "K",
  L: "L",
  M: "M",
  N: "N",
  O: "O",
  P: "P",
  Q: "Q",
  R: "R",
  S: "S",
  T: "T",
  U: "U",
  V: "V",
  W: "W",
  X: "X",
  Y: "Y",
  Z: "Z",

  // Numbers (row)
  Num0: "Num0",
  Num1: "Num1",
  Num2: "Num2",
  Num3: "Num3",
  Num4: "Num4",
  Num5: "Num5",
  Num6: "Num6",
  Num7: "Num7",
  Num8: "Num8",
  Num9: "Num9",

  // Numpad
  Numpad0: "Numpad0",
  Numpad1: "Numpad1",
  Numpad2: "Numpad2",
  Numpad3: "Numpad3",
  Numpad4: "Numpad4",
  Numpad5: "Numpad5",
  Numpad6: "Numpad6",
  Numpad7: "Numpad7",
  Numpad8: "Numpad8",
  Numpad9: "Numpad9",
  NumpadEnter: "NumpadEnter",
  NumpadPlus: "NumpadPlus",
  NumpadMinus: "NumpadMinus",
  NumpadMultiply: "NumpadMultiply",
  NumpadDivide: "NumpadDivide",

  // Function
  F1: "F1",
  F2: "F2",
  F3: "F3",
  F4: "F4",
  F5: "F5",
  F6: "F6",
  F7: "F7",
  F8: "F8",
  F9: "F9",
  F10: "F10",
  F11: "F11",
  F12: "F12",

  // Control
  Enter: "Enter",
  Escape: "Escape",
  Backspace: "Backspace",
  Tab: "Tab",
  Space: "Space",
  Delete: "Delete",
  Insert: "Insert",
  Home: "Home",
  End: "End",
  PageUp: "PageUp",
  PageDown: "PageDown",
  ArrowUp: "ArrowUp",
  ArrowDown: "ArrowDown",
  ArrowLeft: "ArrowLeft",
  ArrowRight: "ArrowRight",
  PrintScreen: "PrintScreen",
  ScrollLock: "ScrollLock",
  Pause: "Pause",

  // Modifiers
  LeftShift: "LeftShift",
  RightShift: "RightShift",
  LeftCtrl: "LeftCtrl",
  RightCtrl: "RightCtrl",
  LeftAlt: "LeftAlt",
  RightAlt: "RightAlt",
  LeftMeta: "LeftMeta",
  RightMeta: "RightMeta", // Win/Cmd key
  CapsLock: "CapsLock",
  NumLock: "NumLock",

  // Punctuation
  Comma: "Comma",
  Period: "Period",
  Slash: "Slash",
  Backslash: "Backslash",
  Semicolon: "Semicolon",
  Quote: "Quote",
  BracketLeft: "BracketLeft",
  BracketRight: "BracketRight",
  Minus: "Minus",
  Equal: "Equal",
  Backtick: "Backtick",

  Unknown: "Unknown",
};

export type KeyCodeEnum = (typeof KeyCodeEnum)[keyof typeof KeyCodeEnum];
