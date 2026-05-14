/**
 * Image Editor Type Definitions
 * TypeScript interfaces for type safety across the image editor
 */

import type { Point, Rectangle } from "electron";

/**
 * Canvas state snapshot for undo/redo
 */
export interface CanvasState {
  imageData: ImageData;
  timestamp: number;
}

/**
 * History entry with action metadata
 */
export interface HistoryEntry {
  state: CanvasState;
  action: string; // e.g., "Crop (Rectangle)", "Erase"
  thumbnail: string; // Base64 data URL
  timestamp: number;
}

/**
 * Result of image editor operation
 */
export interface ImageEditorResult {
  pngBytes: Uint8Array; // PNG as binary bytes
  stepId?: string; // Optional tracking
}

/**
 * Image editor initialization data
 */
export interface ImageEditorInitData {
  dataUrl: string; // PNG as data URL
  stepId?: string; // Optional tracking
}

/**
 * Tool types available in editor
 */
export type EditorTool = "crop-rect" | "crop-lasso" | "eraser" | "select";

/**
 * Crop mode (rectangular or freehand)
 */
export type CropMode = "rect" | "lasso";

/**
 * Canvas operation result
 */
export interface OperationResult {
  success: boolean;
  error?: string;
  data?: any;
}

/**
 * IPC Message for image editor
 */
export interface ImageEditorIpcMessage {
  type: "LOAD_IMAGE" | "RETURN_RESULT" | "READY";
  payload: any;
}

/**
 * Undo/Redo state interface
 */
export interface UndoRedoState {
  currentIndex: number;
  history: HistoryEntry[];
  canUndo: () => boolean;
  canRedo: () => boolean;
}

/**
 * Canvas transformation state
 */
export interface TransformState {
  zoom: number;
  panX: number;
  panY: number;
}

/**
 * Eraser configuration
 */
export interface EraserConfig {
  brushSize: number; // pixels
  softness: 0 | 1 | 2; // 0=hard, 1=soft, 2=very soft
}

/**
 * Crop configuration
 */
export interface CropConfig {
  mode: CropMode;
  minWidth?: number;
  minHeight?: number;
  aspectRatio?: number; // width/height, or undefined for free
}

/**
 * Grid configuration
 */
export interface GridConfig {
  enabled: boolean;
  opacity: number; // 0-1
  size: number; // pixels
  color: string; // hex color
}

/**
 * Color with alpha channel
 */
export interface RGBA {
  r: number; // 0-255
  g: number; // 0-255
  b: number; // 0-255
  a: number; // 0-255 (0=transparent, 255=opaque)
}

/**
 * Brush stroke point with pressure (for future touch support)
 */
export interface StrokePoint {
  x: number;
  y: number;
  pressure?: number; // 0-1 (for touch pen)
  timestamp?: number;
}

/**
 * Keyboard event metadata
 */
export interface KeyboardEventData {
  key: string;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
}

/**
 * Measurement data for operations
 */
export interface Measurements {
  imageWidth: number;
  imageHeight: number;
  selectionWidth?: number;
  selectionHeight?: number;
  selectionArea?: number; // pixels²
}

/**
 * Export options (for future enhancement)
 */
export interface ExportOptions {
  format: "png" | "jpeg" | "webp";
  quality?: number; // 0-100 (for jpeg/webp)
  compression?: number; // 0-9 (for png)
  scale?: number; // export scale (1x, 2x, 0.5x)
}

/**
 * Filter/Effect configuration (for future enhancement)
 */
export interface FilterConfig {
  type: "blur" | "sharpen" | "invert" | "grayscale" | "brightness" | "contrast";
  intensity: number; // 0-100
}

/**
 * API exported to React via ElectronApiService
 */
export interface ImageEditorApi {
  /**
   * Listen for image data to load
   */
  onImageReady: (callback: (data: ImageEditorInitData) => void) => () => void;

  /**
   * Send edited image back to Electron
   */
  returnResult: (result: ImageEditorResult) => void;

  /**
   * Signal that page is ready for image
   */
  signalReady: () => void;
}

/**
 * Operations that can be performed on canvas
 */
export interface CanvasOperations {
  // Transformations
  cropRectangle(rect: Rectangle): void;
  cropLasso(points: Point[]): void;

  // Pixel operations
  erasePixels(points: Point[], brushSize: number): void;
  fillPixels(points: Point[], color: RGBA): void; // future
  applyFilter(filter: FilterConfig): void; // future

  // View operations
  setZoom(zoom: number): void;
  setPan(x: number, y: number): void;
  fitToContainer(container: DOMRect): void;

  // State operations
  saveCanvasState(): CanvasState | null;
  restoreCanvasState(state: CanvasState): void;
}

/**
 * History management interface
 */
export interface HistoryManager {
  recordAction(action: string): void;
  canUndo(): boolean;
  canRedo(): boolean;
  undo(): void;
  redo(): void;
  jumpToIndex(index: number): void;
  getHistory(): HistoryEntry[];
  reset(): void;
}

/**
 * Unified image editor interface (what hooks export)
 */
export interface ImageEditorState {
  // Canvas operations
  cropRectangle(rect: Rectangle): void;
  cropLasso(points: Point[]): void;
  erasePixels(points: Point[], brushSize: number): void;
  saveCanvasState(): CanvasState | null;
  restoreCanvasState(state: CanvasState): void;

  // Transform state
  zoom: number;
  panX: number;
  panY: number;
  isDragging: boolean;

  // Transform operations
  setZoom(zoom: number): void;
  resetZoomPan(): void;
  startPan(x: number, y: number): void;
  updatePan(x: number, y: number): void;
  endPan(): void;
  screenToCanvas(x: number, y: number, container: DOMRect): Point;
  fitToContainer(container: DOMRect): void;
}

// Type guards

export function isValidRectangle(obj: any): obj is Rectangle {
  return (
    typeof obj === "object" &&
    typeof obj.x === "number" &&
    typeof obj.y === "number" &&
    typeof obj.width === "number" &&
    typeof obj.height === "number"
  );
}

export function isValidPoint(obj: any): obj is Point {
  return (
    typeof obj === "object" &&
    typeof obj.x === "number" &&
    typeof obj.y === "number"
  );
}

export function isValidRGBA(obj: any): obj is RGBA {
  return (
    typeof obj === "object" &&
    typeof obj.r === "number" &&
    typeof obj.g === "number" &&
    typeof obj.b === "number" &&
    typeof obj.a === "number" &&
    obj.r >= 0 &&
    obj.r <= 255 &&
    obj.g >= 0 &&
    obj.g <= 255 &&
    obj.b >= 0 &&
    obj.b <= 255 &&
    obj.a >= 0 &&
    obj.a <= 255
  );
}

export function isValidImageEditorResult(obj: any): obj is ImageEditorResult {
  return (
    typeof obj === "object" &&
    obj.pngBytes instanceof Uint8Array &&
    (obj.stepId === undefined || typeof obj.stepId === "string")
  );
}

export function isValidHistoryEntry(obj: any): obj is HistoryEntry {
  return (
    typeof obj === "object" &&
    obj.state &&
    obj.state.imageData instanceof ImageData &&
    typeof obj.action === "string" &&
    typeof obj.thumbnail === "string" &&
    typeof obj.timestamp === "number"
  );
}
