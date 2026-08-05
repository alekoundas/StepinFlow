/**
 * Canvas — the editing viewport.
 *
 * Two stacked canvases sized to the viewport element (never to the image):
 *   - scene   : checkerboard + image + pixel grid, repainted when the image or
 *               the view transform changes
 *   - overlay : selection, lasso path, brush cursor, repainted on pointer move
 *
 * The image is *drawn* through the view transform instead of being CSS
 * transformed, so screen <-> image coordinate maths stays a single formula and
 * a 7680x1440 desktop screenshot never becomes a 7680x1440 DOM element.
 */

import { useCallback, useEffect, useRef, useState } from "react";
import type {
  EditorTool,
  GridOptions,
  PendingSelection,
  Point,
  Size,
} from "@/windows/image-editor/types";
import {
  clampRectToImage,
  distance,
  rectFromPoints,
} from "@/windows/image-editor/utils/canvas-utils";
import type { useViewTransform } from "@/windows/image-editor/hooks/useViewTransform";

const CHECKER_SIZE = 8;
const POLYGON_CLOSE_DISTANCE = 10; // viewport px

type InteractionMode = "none" | "pan" | "rect" | "lasso" | "erase";

interface CanvasProps {
  getDocument: () => HTMLCanvasElement | null;
  imageSize: Size | null;
  revision: number;
  view: ReturnType<typeof useViewTransform>;
  tool: EditorTool;
  grid: GridOptions;
  brushSize: number;
  selection: PendingSelection | null;
  onSelectionChange: (selection: PendingSelection | null) => void;
  onEraseSegment: (from: Point, to: Point, brushSize: number) => void;
  onEraseEnd: () => void;
  onCursorMove: (cursor: Point | null) => void;
  onViewportResize: (size: Size) => void;
}

export default function Canvas({
  getDocument,
  imageSize,
  revision,
  view,
  tool,
  grid,
  brushSize,
  selection,
  onSelectionChange,
  onEraseSegment,
  onEraseEnd,
  onCursorMove,
  onViewportResize,
}: CanvasProps) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<HTMLCanvasElement>(null);
  const overlayRef = useRef<HTMLCanvasElement>(null);
  const checkerRef = useRef<CanvasPattern | null>(null);

  const [viewportSize, setViewportSize] = useState<Size>({
    width: 0,
    height: 0,
  });
  const [cursor, setCursor] = useState<Point | null>(null);
  const [spaceHeld, setSpaceHeld] = useState(false);
  const [panning, setPanning] = useState(false);

  const interaction = useRef<{ mode: InteractionMode; origin: Point; last: Point }>(
    { mode: "none", origin: { x: 0, y: 0 }, last: { x: 0, y: 0 } },
  );

  const { scale, offset, screenToImage, panBy, zoomBy } = view;

  // ==========================================================================
  // Viewport sizing (CSS px) + device pixel ratio backing store
  // ==========================================================================

  useEffect(() => {
    const element = viewportRef.current;
    if (!element) return;

    const observer = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      const size = { width: Math.round(width), height: Math.round(height) };
      setViewportSize(size);
      onViewportResize(size);
    });

    observer.observe(element);
    return () => observer.disconnect();
  }, [onViewportResize]);

  const prepare = useCallback(
    (canvas: HTMLCanvasElement | null): CanvasRenderingContext2D | null => {
      if (!canvas || !viewportSize.width || !viewportSize.height) return null;

      const dpr = window.devicePixelRatio || 1;
      const width = Math.round(viewportSize.width * dpr);
      const height = Math.round(viewportSize.height * dpr);

      if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
      }

      const ctx = canvas.getContext("2d")!;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, viewportSize.width, viewportSize.height);
      return ctx;
    },
    [viewportSize.height, viewportSize.width],
  );

  // ==========================================================================
  // Scene rendering
  // ==========================================================================

  const getCheckerPattern = useCallback(
    (ctx: CanvasRenderingContext2D): CanvasPattern | null => {
      if (checkerRef.current) return checkerRef.current;

      const tile = document.createElement("canvas");
      tile.width = CHECKER_SIZE * 2;
      tile.height = CHECKER_SIZE * 2;
      const tileCtx = tile.getContext("2d")!;
      tileCtx.fillStyle = "#3a3f4b";
      tileCtx.fillRect(0, 0, tile.width, tile.height);
      tileCtx.fillStyle = "#2c313b";
      tileCtx.fillRect(0, 0, CHECKER_SIZE, CHECKER_SIZE);
      tileCtx.fillRect(CHECKER_SIZE, CHECKER_SIZE, CHECKER_SIZE, CHECKER_SIZE);

      checkerRef.current = ctx.createPattern(tile, "repeat");
      return checkerRef.current;
    },
    [],
  );

  const drawGrid = useCallback(
    (ctx: CanvasRenderingContext2D, image: Size) => {
      if (!grid.enabled || scale < grid.minScale) return;

      // Only the visible slice of the image, in image pixel indices.
      const firstX = Math.max(0, Math.floor(-offset.x / scale));
      const lastX = Math.min(
        image.width,
        Math.ceil((viewportSize.width - offset.x) / scale),
      );
      const firstY = Math.max(0, Math.floor(-offset.y / scale));
      const lastY = Math.min(
        image.height,
        Math.ceil((viewportSize.height - offset.y) / scale),
      );
      if (lastX <= firstX || lastY <= firstY) return;

      const left = offset.x + firstX * scale;
      const right = offset.x + lastX * scale;
      const top = offset.y + firstY * scale;
      const bottom = offset.y + lastY * scale;

      ctx.save();
      ctx.lineWidth = 1;

      // Minor lines: one per image pixel.
      ctx.globalAlpha = grid.opacity;
      ctx.strokeStyle = "#ffffff";
      ctx.beginPath();
      for (let x = firstX; x <= lastX; x++) {
        const sx = Math.round(offset.x + x * scale) + 0.5;
        ctx.moveTo(sx, top);
        ctx.lineTo(sx, bottom);
      }
      for (let y = firstY; y <= lastY; y++) {
        const sy = Math.round(offset.y + y * scale) + 0.5;
        ctx.moveTo(left, sy);
        ctx.lineTo(right, sy);
      }
      ctx.stroke();

      // Major lines every 10 image pixels — easier to count against.
      ctx.globalAlpha = Math.min(1, grid.opacity * 2);
      ctx.strokeStyle = "#7dd3fc";
      ctx.beginPath();
      for (let x = firstX - (firstX % 10); x <= lastX; x += 10) {
        if (x < firstX) continue;
        const sx = Math.round(offset.x + x * scale) + 0.5;
        ctx.moveTo(sx, top);
        ctx.lineTo(sx, bottom);
      }
      for (let y = firstY - (firstY % 10); y <= lastY; y += 10) {
        if (y < firstY) continue;
        const sy = Math.round(offset.y + y * scale) + 0.5;
        ctx.moveTo(left, sy);
        ctx.lineTo(right, sy);
      }
      ctx.stroke();
      ctx.restore();
    },
    [
      grid.enabled,
      grid.minScale,
      grid.opacity,
      offset.x,
      offset.y,
      scale,
      viewportSize.height,
      viewportSize.width,
    ],
  );

  const drawScene = useCallback(() => {
    const ctx = prepare(sceneRef.current);
    const source = getDocument();
    if (!ctx || !source || !imageSize) return;

    const width = imageSize.width * scale;
    const height = imageSize.height * scale;

    // Transparency checkerboard, behind the image only.
    const pattern = getCheckerPattern(ctx);
    if (pattern) {
      ctx.save();
      ctx.translate(offset.x, offset.y);
      ctx.fillStyle = pattern;
      ctx.fillRect(0, 0, width, height);
      ctx.restore();
    }

    // Smooth when shrinking, hard pixels when zoomed in.
    ctx.imageSmoothingEnabled = scale < 1;
    ctx.drawImage(source, offset.x, offset.y, width, height);
    ctx.imageSmoothingEnabled = false;

    ctx.strokeStyle = "rgba(255,255,255,0.25)";
    ctx.lineWidth = 1;
    ctx.strokeRect(
      Math.round(offset.x) + 0.5,
      Math.round(offset.y) + 0.5,
      Math.round(width),
      Math.round(height),
    );

    drawGrid(ctx, imageSize);
  }, [
    drawGrid,
    getCheckerPattern,
    getDocument,
    imageSize,
    offset.x,
    offset.y,
    prepare,
    scale,
  ]);

  useEffect(() => {
    drawScene();
  }, [drawScene, revision]);

  // ==========================================================================
  // Overlay rendering (selection / lasso / brush)
  // ==========================================================================

  const drawOverlay = useCallback(() => {
    const ctx = prepare(overlayRef.current);
    if (!ctx) return;

    const toScreen = (point: Point): Point => ({
      x: point.x * scale + offset.x,
      y: point.y * scale + offset.y,
    });

    const tracePath = (closed: boolean) => {
      if (!selection) return;

      if (selection.kind === "rect") {
        const topLeft = toScreen(selection.rect);
        ctx.rect(
          topLeft.x,
          topLeft.y,
          selection.rect.width * scale,
          selection.rect.height * scale,
        );
        return;
      }

      if (selection.points.length === 0) return;
      const first = toScreen(selection.points[0]);
      ctx.moveTo(first.x, first.y);
      for (let i = 1; i < selection.points.length; i++) {
        const point = toScreen(selection.points[i]);
        ctx.lineTo(point.x, point.y);
      }
      // Rubber band to the cursor while a polygon is still open.
      if (!closed && !selection.committed && cursor && tool === "crop-polygon") {
        const live = toScreen(cursor);
        ctx.lineTo(live.x, live.y);
      }
      if (closed) ctx.closePath();
    };

    if (selection) {
      ctx.save();

      // Dim everything outside the selection.
      ctx.fillStyle = "rgba(10, 12, 16, 0.55)";
      ctx.beginPath();
      ctx.rect(0, 0, viewportSize.width, viewportSize.height);
      tracePath(true);
      ctx.fill("evenodd");

      // Outline: solid black underlay + white dashes stays readable on any
      // wallpaper.
      ctx.beginPath();
      tracePath(selection.committed);
      ctx.lineWidth = 1;
      ctx.strokeStyle = "rgba(0,0,0,0.9)";
      ctx.stroke();
      ctx.strokeStyle = "#ffffff";
      ctx.setLineDash([5, 4]);
      ctx.stroke();
      ctx.setLineDash([]);

      // Polygon vertices, so the user can see what they clicked.
      if (selection.kind === "polygon" && tool === "crop-polygon") {
        ctx.fillStyle = "#38bdf8";
        for (const point of selection.points) {
          const screen = toScreen(point);
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, 3, 0, Math.PI * 2);
          ctx.fill();
        }
      }

      ctx.restore();
    }

    // Eraser brush outline.
    if (tool === "eraser" && cursor && !spaceHeld) {
      const screen = toScreen(cursor);
      const radius = Math.max(2, (brushSize * scale) / 2);

      ctx.save();
      ctx.lineWidth = 1;
      ctx.strokeStyle = "rgba(0,0,0,0.85)";
      ctx.beginPath();
      ctx.arc(screen.x, screen.y, radius + 1, 0, Math.PI * 2);
      ctx.stroke();
      ctx.strokeStyle = "#ffffff";
      ctx.beginPath();
      ctx.arc(screen.x, screen.y, radius, 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();
    }
  }, [
    brushSize,
    cursor,
    offset.x,
    offset.y,
    prepare,
    scale,
    selection,
    spaceHeld,
    tool,
    viewportSize.height,
    viewportSize.width,
  ]);

  useEffect(() => {
    drawOverlay();
  }, [drawOverlay]);

  // ==========================================================================
  // Input
  // ==========================================================================

  const toViewportPoint = useCallback((event: React.PointerEvent): Point => {
    const rect = viewportRef.current!.getBoundingClientRect();
    return { x: event.clientX - rect.left, y: event.clientY - rect.top };
  }, []);

  /** Image-space point, snapped to the pixel grid and clamped to the image. */
  const toImagePoint = useCallback(
    (viewportPoint: Point): Point => {
      const raw = screenToImage(viewportPoint);
      if (!imageSize) return { x: Math.round(raw.x), y: Math.round(raw.y) };
      return {
        x: Math.max(0, Math.min(imageSize.width, Math.round(raw.x))),
        y: Math.max(0, Math.min(imageSize.height, Math.round(raw.y))),
      };
    },
    [imageSize, screenToImage],
  );

  const handlePointerDown = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      if (!imageSize) return;

      const viewportPoint = toViewportPoint(event);
      const imagePoint = toImagePoint(viewportPoint);
      const wantsPan =
        event.button === 1 ||
        spaceHeld ||
        (event.button === 0 && tool === "hand");

      event.currentTarget.setPointerCapture(event.pointerId);

      if (wantsPan) {
        interaction.current.mode = "pan";
        interaction.current.last = viewportPoint;
        setPanning(true);
        event.preventDefault();
        return;
      }

      if (event.button !== 0) return;

      switch (tool) {
        case "crop-rect":
          interaction.current.mode = "rect";
          interaction.current.origin = imagePoint;
          onSelectionChange({
            kind: "rect",
            rect: { x: imagePoint.x, y: imagePoint.y, width: 0, height: 0 },
            committed: false,
          });
          break;

        case "crop-lasso":
          interaction.current.mode = "lasso";
          onSelectionChange({
            kind: "polygon",
            points: [imagePoint],
            committed: false,
          });
          break;

        case "crop-polygon": {
          // Click adds a vertex; clicking the first vertex closes the shape.
          const open =
            selection?.kind === "polygon" && !selection.committed
              ? selection.points
              : [];

          if (open.length >= 3) {
            const firstScreen = {
              x: open[0].x * scale + offset.x,
              y: open[0].y * scale + offset.y,
            };
            if (distance(firstScreen, viewportPoint) <= POLYGON_CLOSE_DISTANCE) {
              onSelectionChange({ kind: "polygon", points: open, committed: true });
              break;
            }
          }

          onSelectionChange({
            kind: "polygon",
            points: [...open, imagePoint],
            committed: false,
          });
          break;
        }

        case "eraser":
          interaction.current.mode = "erase";
          interaction.current.last = imagePoint;
          onEraseSegment(imagePoint, imagePoint, brushSize);
          break;

        default:
          break;
      }
    },
    [
      brushSize,
      imageSize,
      offset.x,
      offset.y,
      onEraseSegment,
      onSelectionChange,
      scale,
      selection,
      spaceHeld,
      toImagePoint,
      toViewportPoint,
      tool,
    ],
  );

  const handlePointerMove = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      if (!imageSize) return;

      const viewportPoint = toViewportPoint(event);
      const imagePoint = toImagePoint(viewportPoint);

      setCursor(imagePoint);
      onCursorMove(imagePoint);

      switch (interaction.current.mode) {
        case "pan": {
          panBy(
            viewportPoint.x - interaction.current.last.x,
            viewportPoint.y - interaction.current.last.y,
          );
          interaction.current.last = viewportPoint;
          break;
        }

        case "rect": {
          const rect = clampRectToImage(
            rectFromPoints(interaction.current.origin, imagePoint),
            imageSize,
          );
          onSelectionChange({ kind: "rect", rect, committed: false });
          break;
        }

        case "lasso": {
          if (selection?.kind !== "polygon") break;
          const last = selection.points[selection.points.length - 1];
          // Skip samples landing on the same image pixel.
          if (last && last.x === imagePoint.x && last.y === imagePoint.y) break;
          onSelectionChange({
            kind: "polygon",
            points: [...selection.points, imagePoint],
            committed: false,
          });
          break;
        }

        case "erase": {
          onEraseSegment(interaction.current.last, imagePoint, brushSize);
          interaction.current.last = imagePoint;
          break;
        }

        default:
          break;
      }
    },
    [
      brushSize,
      imageSize,
      onCursorMove,
      onEraseSegment,
      onSelectionChange,
      panBy,
      selection,
      toImagePoint,
      toViewportPoint,
    ],
  );

  const handlePointerUp = useCallback(() => {
    const { mode } = interaction.current;
    interaction.current.mode = "none";
    setPanning(false);

    if (mode === "rect" && selection?.kind === "rect") {
      if (selection.rect.width < 1 || selection.rect.height < 1) {
        onSelectionChange(null);
      } else {
        onSelectionChange({ ...selection, committed: true });
      }
    }

    if (mode === "lasso" && selection?.kind === "polygon") {
      if (selection.points.length < 3) {
        onSelectionChange(null);
      } else {
        onSelectionChange({ ...selection, committed: true });
      }
    }

    if (mode === "erase") onEraseEnd();
  }, [onEraseEnd, onSelectionChange, selection]);

  const handlePointerLeave = useCallback(() => {
    setCursor(null);
    onCursorMove(null);
  }, [onCursorMove]);

  /** Double click closes an in-progress polygon. */
  const handleDoubleClick = useCallback(() => {
    if (
      tool === "crop-polygon" &&
      selection?.kind === "polygon" &&
      !selection.committed &&
      selection.points.length >= 3
    ) {
      onSelectionChange({ ...selection, committed: true });
    }
  }, [onSelectionChange, selection, tool]);

  // Wheel zoom needs a non-passive listener so it can preventDefault.
  useEffect(() => {
    const element = viewportRef.current;
    if (!element) return;

    const onWheel = (event: WheelEvent) => {
      event.preventDefault();
      const rect = element.getBoundingClientRect();
      const anchor = {
        x: event.clientX - rect.left,
        y: event.clientY - rect.top,
      };
      // Normalise line vs pixel deltas so wheel and trackpad agree.
      const delta = event.deltaMode === 1 ? event.deltaY * 16 : event.deltaY;
      zoomBy(Math.exp(-delta * 0.0015), anchor);
    };

    element.addEventListener("wheel", onWheel, { passive: false });
    return () => element.removeEventListener("wheel", onWheel);
  }, [zoomBy]);

  // Hold space to pan with any tool.
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return;

      if (event.code === "Space" && !event.repeat) {
        setSpaceHeld(true);
        event.preventDefault();
      }
    };
    const onKeyUp = (event: KeyboardEvent) => {
      if (event.code === "Space") setSpaceHeld(false);
    };

    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("keyup", onKeyUp);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("keyup", onKeyUp);
    };
  }, []);

  const cursorStyle =
    spaceHeld || tool === "hand"
      ? panning
        ? "grabbing"
        : "grab"
      : tool === "eraser"
        ? "none"
        : "crosshair";

  return (
    <div
      ref={viewportRef}
      className="image-editor__viewport"
      style={{ cursor: cursorStyle }}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={handlePointerUp}
      onPointerLeave={handlePointerLeave}
      onDoubleClick={handleDoubleClick}
      onContextMenu={(event) => event.preventDefault()}
    >
      <canvas ref={sceneRef} className="image-editor__layer" />
      <canvas ref={overlayRef} className="image-editor__layer" />
    </div>
  );
}
