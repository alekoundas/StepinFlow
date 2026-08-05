/**
 * useImageCanvas — owns the image being edited.
 *
 * The image lives in a single detached "document" canvas that is never
 * attached to the DOM; the visible canvas only ever *draws* it through the
 * view transform. That keeps zoom/pan purely a rendering concern and makes
 * every edit operate on real image pixels.
 *
 * Every mutation pushes a snapshot onto the undo/redo stack.
 */

import { useCallback, useRef, useState } from "react";
import type { Point, Rect, Size } from "@/windows/image-editor/types";
import {
  clampRectToImage,
  createCanvas,
  get2dContext,
  polygonBounds,
} from "@/windows/image-editor/utils/canvas-utils";
import { useUndoRedo } from "@/windows/image-editor/hooks/useUndoRedo";

export function useImageCanvas() {
  const documentRef = useRef<HTMLCanvasElement | null>(null);
  const [size, setSize] = useState<Size | null>(null);
  // Bumped on every pixel change so the renderer knows to repaint.
  const [revision, setRevision] = useState(0);
  const history = useUndoRedo();

  const getDocument = useCallback(() => documentRef.current, []);
  const bumpRevision = useCallback(() => setRevision((value) => value + 1), []);

  /** Swap in a new document canvas (crop results, history restores). */
  const setDocument = useCallback(
    (canvas: HTMLCanvasElement) => {
      documentRef.current = canvas;
      setSize({ width: canvas.width, height: canvas.height });
      bumpRevision();
    },
    [bumpRevision],
  );

  // ==========================================================================
  // Loading
  // ==========================================================================

  const loadImage = useCallback(
    (image: HTMLImageElement) => {
      const canvas = createCanvas(image.naturalWidth, image.naturalHeight);
      get2dContext(canvas).drawImage(image, 0, 0);

      setDocument(canvas);
      history.reset(canvas, "Original");
    },
    [history, setDocument],
  );

  // ==========================================================================
  // Edit operations
  // ==========================================================================

  /** Crop to a rectangle. The cropped result becomes the new image. */
  const cropRect = useCallback(
    (rect: Rect) => {
      const source = documentRef.current;
      if (!source) return;

      const bounds = clampRectToImage(rect, {
        width: source.width,
        height: source.height,
      });
      if (bounds.width < 1 || bounds.height < 1) return;

      const cropped = createCanvas(bounds.width, bounds.height);
      get2dContext(cropped).drawImage(source, -bounds.x, -bounds.y);

      setDocument(cropped);
      history.push(cropped, `Crop ${bounds.width}x${bounds.height}`);
    },
    [history, setDocument],
  );

  /**
   * Crop to a freehand / polygon selection: the result is the bounding box of
   * the polygon, with everything outside the polygon made transparent.
   */
  const cropPolygon = useCallback(
    (points: Point[]) => {
      const source = documentRef.current;
      if (!source || points.length < 3) return;

      const bounds = clampRectToImage(polygonBounds(points), {
        width: source.width,
        height: source.height,
      });
      if (bounds.width < 1 || bounds.height < 1) return;

      const cropped = createCanvas(bounds.width, bounds.height);
      const ctx = get2dContext(cropped);

      ctx.beginPath();
      ctx.moveTo(points[0].x - bounds.x, points[0].y - bounds.y);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x - bounds.x, points[i].y - bounds.y);
      }
      ctx.closePath();
      ctx.clip();
      ctx.drawImage(source, -bounds.x, -bounds.y);

      setDocument(cropped);
      history.push(cropped, `Lasso crop ${bounds.width}x${bounds.height}`);
    },
    [history, setDocument],
  );

  /**
   * Erase (make transparent) along a stroke segment.
   * Called repeatedly while dragging — no history entry until `endErase`.
   */
  const eraseSegment = useCallback(
    (from: Point, to: Point, brushSize: number) => {
      const canvas = documentRef.current;
      if (!canvas) return;

      const ctx = get2dContext(canvas);
      ctx.save();
      ctx.globalCompositeOperation = "destination-out";
      ctx.lineWidth = Math.max(1, brushSize);
      ctx.lineCap = "round";
      ctx.lineJoin = "round";
      ctx.strokeStyle = "rgba(0,0,0,1)";

      ctx.beginPath();
      ctx.moveTo(from.x + 0.5, from.y + 0.5);
      ctx.lineTo(to.x + 0.5, to.y + 0.5);
      ctx.stroke();
      ctx.restore();

      bumpRevision();
    },
    [bumpRevision],
  );

  const endErase = useCallback(() => {
    const canvas = documentRef.current;
    if (!canvas) return;
    history.push(canvas, "Erase");
  }, [history]);

  // ==========================================================================
  // History
  // ==========================================================================

  /** Restore a snapshot without recording a new history entry. */
  const restoreSnapshot = useCallback(
    (snapshot: HTMLCanvasElement) => {
      const restored = createCanvas(snapshot.width, snapshot.height);
      get2dContext(restored).drawImage(snapshot, 0, 0);
      setDocument(restored);
    },
    [setDocument],
  );

  const jumpToHistory = useCallback(
    (index: number) => {
      const entry = history.jumpTo(index);
      if (entry) restoreSnapshot(entry.canvas);
    },
    [history, restoreSnapshot],
  );

  const undo = useCallback(() => {
    const entry = history.undo();
    if (entry) restoreSnapshot(entry.canvas);
  }, [history, restoreSnapshot]);

  const redo = useCallback(() => {
    const entry = history.redo();
    if (entry) restoreSnapshot(entry.canvas);
  }, [history, restoreSnapshot]);

  return {
    // document
    getDocument,
    size,
    revision,
    loadImage,

    // operations
    cropRect,
    cropPolygon,
    eraseSegment,
    endErase,

    // history
    history,
    undo,
    redo,
    jumpToHistory,
  };
}
