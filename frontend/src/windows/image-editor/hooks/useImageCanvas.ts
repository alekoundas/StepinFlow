/**
 * useImageCanvas - Image manipulation hook
 *
 * Manages:
 *  - Canvas rendering and state
 *  - Zoom & Pan transformations
 *  - Image operations (crop, erase)
 *  - Canvas save/restore for undo/redo
 *
 * Uses Canvas 2D API for all image operations.
 * State stored as ImageData for fast undo/redo.
 */

import { useCallback, useRef, useState } from "react";

interface CanvasState {
  imageData: ImageData;
  timestamp: number;
}

export function useImageCanvas(
  canvasRef: React.RefObject<HTMLCanvasElement | null>,
) {
  // ======================================================================
  // Zoom & Pan state
  // ======================================================================
  const [zoom, setZoom] = useState(1);
  const [panX, setPanX] = useState(0);
  const [panY, setPanY] = useState(0);
  const [isDragging, setIsDragging] = useState(false);
  const dragStartRef = useRef({ x: 0, y: 0, panX: 0, panY: 0 });

  // ======================================================================
  // Canvas state for operations
  // ======================================================================
  const canvasStateRef = useRef<CanvasState | null>(null);

  /**
   * Save current canvas state for undo/redo
   */
  const saveCanvasState = useCallback((): CanvasState | null => {
    const canvas = canvasRef.current;
    if (!canvas) return null;

    const ctx = canvas.getContext("2d");
    if (!ctx) return null;

    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    canvasStateRef.current = {
      imageData,
      timestamp: Date.now(),
    };

    return canvasStateRef.current;
  }, []);

  /**
   * Restore canvas from saved state (used by undo/redo)
   */
  const restoreCanvasState = useCallback((state: CanvasState) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    ctx.putImageData(state.imageData, 0, 0);
  }, []);

  /**
   * Reset zoom and pan to default
   */
  const resetZoomPan = useCallback(() => {
    setZoom(1);
    setPanX(0);
    setPanY(0);
  }, []);

  // ======================================================================
  // Image Operations
  // ======================================================================

  /**
   * Crop to rectangle - canvas in local coordinates
   */
  const cropRectangle = useCallback(
    (rect: { x: number; y: number; width: number; height: number }) => {
      const canvas = canvasRef.current;
      if (!canvas) return;

      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      // Clamp to canvas bounds
      const x = Math.max(0, Math.floor(rect.x));
      const y = Math.max(0, Math.floor(rect.y));
      const w = Math.min(canvas.width - x, Math.ceil(rect.width));
      const h = Math.min(canvas.height - y, Math.ceil(rect.height));

      if (w <= 0 || h <= 0) return;

      // Get cropped image data
      const imageData = ctx.getImageData(x, y, w, h);

      // Resize canvas and draw cropped region
      canvas.width = w;
      canvas.height = h;
      ctx.putImageData(imageData, 0, 0);

      // Reset view after crop
      resetZoomPan();
    },
    [resetZoomPan],
  );

  /**
   * Crop using lasso (polygon) - points in canvas coords
   */
  const cropLasso = useCallback((points: Array<{ x: number; y: number }>) => {
    const canvas = canvasRef.current;
    if (!canvas || points.length < 3) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    // Find bounding box of polygon
    let minX = Infinity,
      minY = Infinity,
      maxX = -Infinity,
      maxY = -Infinity;
    for (const p of points) {
      minX = Math.min(minX, p.x);
      minY = Math.min(minY, p.y);
      maxX = Math.max(maxX, p.x);
      maxY = Math.max(maxY, p.y);
    }

    const bbX = Math.floor(minX);
    const bbY = Math.floor(minY);
    const bbW = Math.ceil(maxX - minX) + 1;
    const bbH = Math.ceil(maxY - minY) + 1;

    // Create mask canvas for lasso selection
    const maskCanvas = document.createElement("canvas");
    maskCanvas.width = canvas.width;
    maskCanvas.height = canvas.height;
    const maskCtx = maskCanvas.getContext("2d");
    if (!maskCtx) return;

    maskCtx.fillStyle = "black";
    maskCtx.fillRect(0, 0, maskCanvas.width, maskCanvas.height);

    // Draw white polygon on mask (will be the selected area)
    maskCtx.fillStyle = "white";
    maskCtx.beginPath();
    maskCtx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
      maskCtx.lineTo(points[i].x, points[i].y);
    }
    maskCtx.closePath();
    maskCtx.fill();

    // Get mask data
    const maskData = maskCtx.getImageData(
      0,
      0,
      maskCanvas.width,
      maskCanvas.height,
    );
    const originalData = ctx.getImageData(0, 0, canvas.width, canvas.height);

    // Apply mask - set alpha to 0 where mask is black
    const origBytes = originalData.data;
    const maskBytes = maskData.data;
    for (let i = 0; i < origBytes.length; i += 4) {
      const maskIdx = i;
      if (maskBytes[maskIdx] === 0) {
        origBytes[i + 3] = 0; // Set alpha to 0 (transparent)
      }
    }

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.putImageData(originalData, 0, 0);
  }, []);

  /**
   * Erase pixels (make transparent) along a path with brush
   */
  const erasePixels = useCallback(
    (points: Array<{ x: number; y: number }>, brushSize: number) => {
      const canvas = canvasRef.current;
      if (!canvas || points.length === 0) return;

      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
      const data = imageData.data;

      // For each point in the stroke, erase in a circle
      for (const point of points) {
        const radius = brushSize / 2;
        const x = Math.floor(point.x);
        const y = Math.floor(point.y);

        // Erase circular area
        for (let dy = -radius; dy <= radius; dy++) {
          for (let dx = -radius; dx <= radius; dx++) {
            const px = x + dx;
            const py = y + dy;

            // Check if within canvas and within circular brush
            if (
              px >= 0 &&
              px < canvas.width &&
              py >= 0 &&
              py < canvas.height &&
              dx * dx + dy * dy <= radius * radius
            ) {
              const idx = (py * canvas.width + px) * 4;
              data[idx + 3] = 0; // Set alpha to 0
            }
          }
        }
      }

      ctx.putImageData(imageData, 0, 0);
    },
    [],
  );

  // ======================================================================
  // Zoom & Pan
  // ======================================================================

  /**
   * Calculate world coordinates from screen coordinates
   * Used to convert mouse events to canvas-local coordinates
   */
  const screenToCanvas = useCallback(
    (
      screenX: number,
      screenY: number,
      containerRect: DOMRect,
    ): { x: number; y: number } => {
      const x = (screenX - containerRect.left - panX) / zoom;
      const y = (screenY - containerRect.top - panY) / zoom;
      return { x, y };
    },
    [zoom, panX, panY],
  );

  /**
   * Start pan drag
   */
  const startPan = useCallback(
    (screenX: number, screenY: number) => {
      setIsDragging(true);
      dragStartRef.current = { x: screenX, y: screenY, panX, panY };
    },
    [panX, panY],
  );

  /**
   * Update pan during drag
   */
  const updatePan = useCallback(
    (screenX: number, screenY: number) => {
      if (!isDragging) return;

      const dx = screenX - dragStartRef.current.x;
      const dy = screenY - dragStartRef.current.y;

      setPanX(dragStartRef.current.panX + dx);
      setPanY(dragStartRef.current.panY + dy);
    },
    [isDragging],
  );

  /**
   * End pan drag
   */
  const endPan = useCallback(() => {
    setIsDragging(false);
  }, []);

  /**
   * Change zoom level (e.g., 0.5 = 50%, 2 = 200%)
   */
  const updateZoom = useCallback((newZoom: number) => {
    setZoom(Math.max(0.1, Math.min(newZoom, 10)));
  }, []);

  /**
   * Fit canvas to container
   */
  const fitToContainer = useCallback((containerRect: DOMRect) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const maxZoom = Math.min(
      containerRect.width / canvas.width,
      containerRect.height / canvas.height,
    );

    setZoom(Math.max(0.1, Math.min(maxZoom, 1)));
    setPanX(0);
    setPanY(0);
  }, []);

  return {
    // Zoom & Pan
    zoom,
    setZoom: updateZoom,
    panX,
    panY,
    isDragging,
    screenToCanvas,
    startPan,
    updatePan,
    endPan,
    resetZoomPan,
    fitToContainer,

    // Canvas state
    saveCanvasState,
    restoreCanvasState,

    // Image operations
    cropRectangle,
    cropLasso,
    erasePixels,
  };
}
