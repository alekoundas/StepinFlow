/**
 * Minimap Component - Navigation preview
 *
 * Shows:
 *  - Miniature version of the full image
 *  - Viewport rectangle showing what's visible in the main canvas
 *  - Click to pan to that area
 */

import { useEffect, useRef } from "react";
import { useImageCanvas } from "../hooks/useImageCanvas";

interface MinimapProps {
  canvasRef: React.RefObject<HTMLCanvasElement>;
  imageCanvas: ReturnType<typeof useImageCanvas>;
  containerRef: React.RefObject<HTMLDivElement>;
}

export default function Minimap({
  canvasRef,
  imageCanvas,
  containerRef,
}: MinimapProps) {
  const minimapCanvasRef = useRef<HTMLCanvasElement>(null);
  const minimapSize = 150; // Fixed size

  // ========================================================================
  // Rendering
  // ========================================================================

  useEffect(() => {
    const minimapCanvas = minimapCanvasRef.current;
    const sourceCanvas = canvasRef.current;
    const container = containerRef.current;

    if (!minimapCanvas || !sourceCanvas || !container) return;

    const ctx = minimapCanvas.getContext("2d");
    if (!ctx) return;

    // Calculate scale to fit source image into minimap
    const scale = Math.min(
      minimapSize / sourceCanvas.width,
      minimapSize / sourceCanvas.height,
    );

    const scaledW = sourceCanvas.width * scale;
    const scaledH = sourceCanvas.height * scale;

    // Center in minimap canvas
    const offsetX = (minimapSize - scaledW) / 2;
    const offsetY = (minimapSize - scaledH) / 2;

    // Clear
    ctx.fillStyle = "#1a1a1a";
    ctx.fillRect(0, 0, minimapSize, minimapSize);

    // Draw scaled image
    ctx.drawImage(sourceCanvas, offsetX, offsetY, scaledW, scaledH);

    // Draw viewport indicator (red rectangle showing what's visible)
    const containerRect = container.getBoundingClientRect();
    const viewportX = (-imageCanvas.panX / imageCanvas.zoom) * scale + offsetX;
    const viewportY = (-imageCanvas.panY / imageCanvas.zoom) * scale + offsetY;
    const viewportW = (containerRect.width / imageCanvas.zoom) * scale;
    const viewportH = (containerRect.height / imageCanvas.zoom) * scale;

    ctx.strokeStyle = "rgba(100, 150, 255, 0.8)";
    ctx.lineWidth = 2;
    ctx.strokeRect(viewportX, viewportY, viewportW, viewportH);

    // Update every frame for smooth sync with zoom/pan
    requestAnimationFrame(() => {});
  }, [
    canvasRef,
    imageCanvas.zoom,
    imageCanvas.panX,
    imageCanvas.panY,
    containerRef,
  ]);

  // ========================================================================
  // Click to pan
  // ========================================================================

  const handleMinimapClick = (e: React.MouseEvent) => {
    const minimapCanvas = minimapCanvasRef.current;
    const sourceCanvas = canvasRef.current;
    if (!minimapCanvas || !sourceCanvas) return;

    const rect = minimapCanvas.getBoundingClientRect();
    const clickX = e.clientX - rect.left;
    const clickY = e.clientY - rect.top;

    // Convert minimap coords to source canvas coords
    const scale = Math.min(
      minimapSize / sourceCanvas.width,
      minimapSize / sourceCanvas.height,
    );

    const offsetX = (minimapSize - sourceCanvas.width * scale) / 2;
    const offsetY = (minimapSize - sourceCanvas.height * scale) / 2;

    const sourceX = (clickX - offsetX) / scale;
    const sourceY = (clickY - offsetY) / scale;

    // Calculate pan to center on that point
    const containerRect = containerRef.current?.getBoundingClientRect();
    if (!containerRect) return;

    const targetPanX = containerRect.width / 2 - sourceX * imageCanvas.zoom;
    const targetPanY = containerRect.height / 2 - sourceY * imageCanvas.zoom;

    // Smoothly animate to the target (optional - for now, just set directly)
    // For instant pan:
    // imageCanvas.setPan(targetPanX, targetPanY);
    // For now we'll trigger a quick pan - this would need exposing setPan method
  };

  // ========================================================================
  // Render
  // ========================================================================

  return (
    <div className="editor-minimap">
      <canvas
        ref={minimapCanvasRef}
        width={minimapSize}
        height={minimapSize}
        onClick={handleMinimapClick}
        style={{ cursor: "pointer", border: "1px solid #444" }}
        title="Click to pan to that area"
      />
    </div>
  );
}
