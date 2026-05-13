/**
 * useUndoRedo - Undo/Redo management hook
 *
 * Maintains a stack of:
 *  - Canvas state (ImageData)
 *  - Action description
 *  - Thumbnail for UI
 *
 * Enables jumping to any point in history,
 * with thumbnails for quick preview.
 */

import { useCallback, useRef, useState } from "react";

interface HistoryEntry {
  state: any; // Canvas state from saveCanvasState()
  action: string; // Description like "Crop (Rectangle)"
  thumbnail: string; // Base64 data URL for thumbnail
  timestamp: number;
}

export function useUndoRedo(saveCanvasState: () => any) {
  // ======================================================================
  // State
  // ======================================================================
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [currentIndex, setCurrentIndex] = useState(-1);

  // ======================================================================
  // Public API
  // ======================================================================

  /**
   * Record an action with current canvas state
   * Called after user performs an edit (crop, erase, etc.)
   */
  const recordAction = useCallback(
    (action: string) => {
      const state = saveCanvasState();
      if (!state) return;

      // Generate thumbnail from canvas
      const thumbnail = generateThumbnail(state);

      const entry: HistoryEntry = {
        state,
        action,
        thumbnail,
        timestamp: Date.now(),
      };

      // Remove all entries after current index (discard "future" if user undid then did something new)
      const newHistory = history.slice(0, currentIndex + 1);
      newHistory.push(entry);

      setHistory(newHistory);
      setCurrentIndex(newHistory.length - 1);
    },
    [history, currentIndex, saveCanvasState],
  );

  /**
   * Check if undo is possible
   */
  const canUndo = useCallback(() => currentIndex > 0, [currentIndex]);

  /**
   * Check if redo is possible
   */
  const canRedo = useCallback(
    () => currentIndex < history.length - 1,
    [currentIndex, history.length],
  );

  /**
   * Undo to previous state
   */
  const undo = useCallback(() => {
    if (currentIndex > 0) {
      setCurrentIndex(currentIndex - 1);
    }
  }, [currentIndex]);

  /**
   * Redo to next state
   */
  const redo = useCallback(() => {
    if (currentIndex < history.length - 1) {
      setCurrentIndex(currentIndex + 1);
    }
  }, [currentIndex, history.length]);

  /**
   * Jump to specific history index
   */
  const jumpToIndex = useCallback(
    (index: number) => {
      if (index >= 0 && index < history.length) {
        setCurrentIndex(index);
      }
    },
    [history.length],
  );

  /**
   * Reset history (for new image)
   */
  const reset = useCallback(() => {
    setHistory([]);
    setCurrentIndex(-1);
  }, []);

  /**
   * Get all history entries for display
   */
  const getHistory = useCallback(() => history, [history]);

  /**
   * Get current state (for restore)
   */
  const currentState = currentIndex >= 0 ? history[currentIndex]?.state : null;

  return {
    recordAction,
    canUndo,
    canRedo,
    undo,
    redo,
    jumpToIndex,
    reset,
    getHistory,
    currentIndex,
    currentState,
  };
}

/**
 * Generate a thumbnail from canvas state
 * Creates a small (100x100) preview for the history panel
 */
function generateThumbnail(canvasState: any): string {
  if (!canvasState?.imageData) return "";

  const thumbSize = 100;
  const thumbCanvas = document.createElement("canvas");
  thumbCanvas.width = thumbSize;
  thumbCanvas.height = thumbSize;
  const ctx = thumbCanvas.getContext("2d");
  if (!ctx) return "";

  // Get source dimensions from ImageData
  const srcWidth = canvasState.imageData.width;
  const srcHeight = canvasState.imageData.height;

  // Calculate scale to fit in thumbnail
  const scale = Math.min(thumbSize / srcWidth, thumbSize / srcHeight);
  const scaledW = srcWidth * scale;
  const scaledH = srcHeight * scale;

  // Center in thumbnail
  const offsetX = (thumbSize - scaledW) / 2;
  const offsetY = (thumbSize - scaledH) / 2;

  // Create temporary canvas to hold source image
  const tempCanvas = document.createElement("canvas");
  tempCanvas.width = srcWidth;
  tempCanvas.height = srcHeight;
  const tempCtx = tempCanvas.getContext("2d");
  if (!tempCtx) return "";

  tempCtx.putImageData(canvasState.imageData, 0, 0);

  // Draw scaled source onto thumbnail
  ctx.drawImage(tempCanvas, offsetX, offsetY, scaledW, scaledH);

  return thumbCanvas.toDataURL("image/png");
}
