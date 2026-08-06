/**
 * Minimap — whole image at a glance plus the current viewport rectangle.
 * Click or drag inside it to move the view.
 */

import { useCallback, useEffect, useRef } from "react";
import type { Point, Size } from "@/windows/image-editor/types";
import type { useViewTransform } from "@/windows/image-editor/hooks/useViewTransform";

const MAX_WIDTH = 200;
const MAX_HEIGHT = 140;

interface MinimapProps {
  getDocument: () => HTMLCanvasElement | null;
  imageSize: Size | null;
  revision: number;
  view: ReturnType<typeof useViewTransform>;
  viewportSize: Size;
}

export default function Minimap({
  getDocument,
  imageSize,
  revision,
  view,
  viewportSize,
}: MinimapProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const draggingRef = useRef(false);

  const ratio = imageSize
    ? Math.min(MAX_WIDTH / imageSize.width, MAX_HEIGHT / imageSize.height, 1)
    : 1;

  const { scale, offset } = view;

  useEffect(() => {
    const canvas = canvasRef.current;
    const source = getDocument();
    if (!canvas || !source || !imageSize) return;

    const width = Math.max(1, Math.round(imageSize.width * ratio));
    const height = Math.max(1, Math.round(imageSize.height * ratio));
    const dpr = window.devicePixelRatio || 1;

    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);

    const ctx = canvas.getContext("2d")!;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, width, height);
    ctx.imageSmoothingEnabled = true;
    ctx.drawImage(source, 0, 0, width, height);

    // Viewport rectangle, in image space then scaled to the minimap.
    const left = (-offset.x / scale) * ratio;
    const top = (-offset.y / scale) * ratio;
    const boxWidth = (viewportSize.width / scale) * ratio;
    const boxHeight = (viewportSize.height / scale) * ratio;

    ctx.save();
    ctx.strokeStyle = "#38bdf8";
    ctx.lineWidth = 1.5;
    ctx.strokeRect(left, top, boxWidth, boxHeight);
    ctx.fillStyle = "rgba(56, 189, 248, 0.15)";
    ctx.fillRect(left, top, boxWidth, boxHeight);
    ctx.restore();
  }, [
    getDocument,
    imageSize,
    offset.x,
    offset.y,
    ratio,
    revision,
    scale,
    viewportSize.height,
    viewportSize.width,
  ]);

  const moveViewTo = useCallback(
    (event: React.PointerEvent<HTMLCanvasElement>) => {
      if (!imageSize) return;
      const bounds = event.currentTarget.getBoundingClientRect();
      const imagePoint: Point = {
        x: (event.clientX - bounds.left) / ratio,
        y: (event.clientY - bounds.top) / ratio,
      };
      view.centerOn(imagePoint, viewportSize);
    },
    [imageSize, ratio, view, viewportSize],
  );

  const handlePointerDown = useCallback(
    (event: React.PointerEvent<HTMLCanvasElement>) => {
      draggingRef.current = true;
      event.currentTarget.setPointerCapture(event.pointerId);
      moveViewTo(event);
    },
    [moveViewTo],
  );

  const handlePointerMove = useCallback(
    (event: React.PointerEvent<HTMLCanvasElement>) => {
      if (draggingRef.current) moveViewTo(event);
    },
    [moveViewTo],
  );

  const handlePointerUp = useCallback(() => {
    draggingRef.current = false;
  }, []);

  if (!imageSize) return null;

  return (
    <div className="absolute right-0 bottom-0 m-3 p-2 surface-overlay border-1 surface-border border-round shadow-4">
      <div
        className="mb-1 text-xs uppercase text-color-secondary"
        style={{ letterSpacing: "0.04em" }}
      >
        Minimap
      </div>
      <canvas
        ref={canvasRef}
        className="block cursor-pointer border-round-xs"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerCancel={handlePointerUp}
      />
    </div>
  );
}
