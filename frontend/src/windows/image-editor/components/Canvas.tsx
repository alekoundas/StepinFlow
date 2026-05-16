/**
 * Canvas Component - Main editing surface
 *
 * Handles:
 *  - Tool-specific interactions (crop, erase, select/pan)
 *  - Rendering with zoom/pan transformation
 *  - Pixel grid overlay
 *  - Crop visualization
 *
 * Uses requestAnimationFrame for smooth rendering
 */

import { useEffect, useRef, useState } from "react";
import { useImageCanvas } from "../hooks/useImageCanvas";

interface CanvasProps {
  imageDataUrl: string | null;
  activeTool: "crop-rect" | "crop-lasso" | "eraser" | "select";
  cropMode: "rect" | "lasso";
  showGrid: boolean;
  gridOpacity: number;
  imageCanvas: ReturnType<typeof useImageCanvas>;
  onCropRectFinished: (rect: {
    x: number;
    y: number;
    width: number;
    height: number;
  }) => void;
  onCropLassoFinished: (points: Array<{ x: number; y: number }>) => void;
  onErasePixels: (
    points: Array<{ x: number; y: number }>,
    brushSize: number,
  ) => void;
}

export default function Canvas({
  imageDataUrl,
  activeTool,
  cropMode,
  showGrid,
  gridOpacity,
  imageCanvas,
  onCropRectFinished,
  onCropLassoFinished,
  onErasePixels,
}: CanvasProps) {
  // ======================================================================
  // Interaction state
  // ======================================================================

  const [isInteracting, setIsInteracting] = useState(false);
  const [cropStart, setCropStart] = useState<{ x: number; y: number } | null>(
    null,
  );
  const [cropCurrent, setCropCurrent] = useState<{
    x: number;
    y: number;
  } | null>(null);
  const [lassoPoints, setLassoPoints] = useState<
    Array<{ x: number; y: number }>
  >([]);
  const [eraseStroke, setEraseStroke] = useState<
    Array<{ x: number; y: number }>
  >([]);
  const brushSizeRef = useRef(10);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    if (!imageDataUrl || !canvasRef.current) return;
    const img = new Image();

    // Attach handlers BEFORE setting src
    img.onload = () => {
      const canvas = canvasRef.current!;
      canvas.width = img.width;
      canvas.height = img.height;

      const ctx = canvas.getContext("2d");
      if (ctx) {
        ctx.drawImage(img, 500, 500);
        imageCanvas.resetZoomPan();
      }
    };

    img.onerror = (event: Event | string) => {
      console.error("[ImageEditor] Failed to load image. Event:", event);
    };

    // Set src AFTER handlers are attached
    img.src = imageDataUrl;
  }, [imageDataUrl]); // Empty deps: runs only once on mount

  // ======================================================================
  // Rendering - Display canvas with zoom/pan + grid
  // ======================================================================

  /**
   * Render the canvas with transformations and overlays
   * Called every frame to show:
   *  - Zoomed/panned image
   *  - Pixel grid (if enabled)
   *  - Crop previews
   *  - Erase preview
   */
  const render = useRef(() => {
    if (!canvasRef.current) return;

    const ctx = canvasRef.current.getContext("2d");
    if (!ctx) return;

    // Clear
    ctx.fillStyle = "#2a2a2a"; // Dark background
    ctx.fillRect(0, 0, canvasRef.current.width, canvasRef.current.height);

    // Save state for transformations
    ctx.save();
    ctx.translate(imageCanvas.panX, imageCanvas.panY);
    ctx.scale(imageCanvas.zoom, imageCanvas.zoom);

    // Draw source image
    ctx.drawImage(canvasRef.current, 0, 0);

    // Draw pixel grid if enabled
    if (showGrid && imageCanvas.zoom > 3) {
      drawPixelGrid(
        ctx,
        canvasRef.current.width,
        canvasRef.current.height,
        gridOpacity,
      );
    }

    // Draw crop preview (rect or lasso)
    if (activeTool.startsWith("crop") && cropStart) {
      ctx.globalAlpha = 0.3;
      ctx.fillStyle = "rgba(100, 150, 255, 0.5)";

      if (cropMode === "rect" && cropCurrent) {
        const rect = {
          x: Math.min(cropStart.x, cropCurrent.x),
          y: Math.min(cropStart.y, cropCurrent.y),
          width: Math.abs(cropCurrent.x - cropStart.x),
          height: Math.abs(cropCurrent.y - cropStart.y),
        };
        ctx.fillRect(rect.x, rect.y, rect.width, rect.height);

        // Dim everything outside selection
        ctx.globalAlpha = 0.5;
        ctx.fillStyle = "rgba(0, 0, 0, 1)";
        ctx.fillRect(0, 0, canvasRef.current.width, rect.y);
        ctx.fillRect(0, rect.y, rect.x, rect.height);
        ctx.fillRect(
          rect.x + rect.width,
          rect.y,
          canvasRef.current.width - (rect.x + rect.width),
          rect.height,
        );
        ctx.fillRect(
          0,
          rect.y + rect.height,
          canvasRef.current.width,
          canvasRef.current.height - (rect.y + rect.height),
        );
      } else if (cropMode === "lasso" && lassoPoints.length > 0) {
        // Draw lasso path
        ctx.globalAlpha = 1;
        ctx.strokeStyle = "rgba(100, 150, 255, 1)";
        ctx.lineWidth = 2 / imageCanvas.zoom; // Adjust for zoom
        ctx.beginPath();
        ctx.moveTo(lassoPoints[0].x, lassoPoints[0].y);
        for (let i = 1; i < lassoPoints.length; i++) {
          ctx.lineTo(lassoPoints[i].x, lassoPoints[i].y);
        }
        ctx.closePath();
        ctx.stroke();
      }
    }

    ctx.restore();

    // Schedule next frame
    requestAnimationFrame(render.current);
  });

  useEffect(() => {
    requestAnimationFrame(render.current);
  }, [
    showGrid,
    gridOpacity,
    imageCanvas.zoom,
    imageCanvas.panX,
    imageCanvas.panY,
    activeTool,
    cropMode,
  ]);

  // Size display canvas to match container
  // useEffect(() => {
  //   const resizeDisplayCanvas = () => {
  //     if (!displayCanvasRef.current || !containerRef.current) return;

  //     const rect = containerRef.current.getBoundingClientRect();
  //     displayCanvasRef.current.width = rect.width;
  //     displayCanvasRef.current.height = rect.height;
  //   };

  //   resizeDisplayCanvas();

  //   // Resize on window resize
  //   window.addEventListener("resize", resizeDisplayCanvas);
  //   return () => window.removeEventListener("resize", resizeDisplayCanvas);
  // }, []);

  // ======================================================================
  // Mouse/Touch events
  // ======================================================================

  const handleMouseDown = (e: React.MouseEvent) => {
    if (!canvasRef.current) return;

    const canvasRect = canvasRef.current.getBoundingClientRect();
    const canvasCoords = imageCanvas.screenToCanvas(
      e.clientX,
      e.clientY,
      canvasRect,
    );

    if (activeTool === "select") {
      // Pan/drag
      imageCanvas.startPan(e.clientX, e.clientY);
      setIsInteracting(true);
    } else if (
      activeTool === "crop-rect" ||
      (activeTool === "crop-lasso" && cropMode === "rect")
    ) {
      // Rectangular crop start
      setCropStart(canvasCoords);
      setCropCurrent(canvasCoords);
      setLassoPoints([]);
      setIsInteracting(true);
    } else if (activeTool === "crop-lasso" && cropMode === "lasso") {
      // Lasso crop - start new or add to path
      const newPoints = [...lassoPoints, canvasCoords];
      setLassoPoints(newPoints);
      setIsInteracting(true);
    } else if (activeTool === "eraser") {
      // Eraser start
      setEraseStroke([canvasCoords]);
      setIsInteracting(true);
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!canvasRef.current) return;

    const canvasRect = canvasRef.current.getBoundingClientRect();
    const canvasCoords = imageCanvas.screenToCanvas(
      e.clientX,
      e.clientY,
      canvasRect,
    );

    if (activeTool === "select") {
      if (isInteracting) {
        imageCanvas.updatePan(e.clientX, e.clientY);
      }
    } else if (
      activeTool === "crop-rect" ||
      (activeTool === "crop-lasso" && cropMode === "rect")
    ) {
      if (isInteracting) {
        setCropCurrent(canvasCoords);
      }
    } else if (activeTool === "eraser") {
      if (isInteracting) {
        setEraseStroke((prev) => [...prev, canvasCoords]);
      }
    }

    // Update cursor
    // if (activeTool === "select") {
    //   containerRef.current.style.cursor = imageCanvas.isDragging
    //     ? "grabbing"
    //     : "grab";
    // } else if (activeTool === "crop-rect" || activeTool === "crop-lasso") {
    //   containerRef.current.style.cursor = "crosshair";
    // } else if (activeTool === "eraser") {
    //   containerRef.current.style.cursor = `radial-gradient(circle, rgba(255,255,255,0.5) 0%, transparent ${brushSizeRef.current}px)`;
    // }
  };

  const handleMouseUp = (e: React.MouseEvent) => {
    if (!canvasRef.current) return;

    const canvasRect = canvasRef.current.getBoundingClientRect();
    const canvasCoords = imageCanvas.screenToCanvas(
      e.clientX,
      e.clientY,
      canvasRect,
    );

    if (activeTool === "select") {
      imageCanvas.endPan();
      setIsInteracting(false);
    } else if (
      activeTool === "crop-rect" ||
      (activeTool === "crop-lasso" && cropMode === "rect")
    ) {
      if (cropStart && cropCurrent) {
        const rect = {
          x: Math.min(cropStart.x, cropCurrent.x),
          y: Math.min(cropStart.y, cropCurrent.y),
          width: Math.abs(cropCurrent.x - cropStart.x),
          height: Math.abs(cropCurrent.y - cropStart.y),
        };

        if (rect.width > 5 && rect.height > 5) {
          onCropRectFinished(rect);
        }
      }
      setCropStart(null);
      setCropCurrent(null);
      setLassoPoints([]);
      setIsInteracting(false);
    } else if (activeTool === "eraser") {
      if (eraseStroke.length > 0) {
        onErasePixels(eraseStroke, brushSizeRef.current);
      }
      setEraseStroke([]);
      setIsInteracting(false);
    }
  };

  /**
   * Double-click in lasso mode finishes the path
   */
  const handleDoubleClick = (e: React.MouseEvent) => {
    if (
      activeTool === "crop-lasso" &&
      cropMode === "lasso" &&
      lassoPoints.length > 2
    ) {
      onCropLassoFinished(lassoPoints);
      setLassoPoints([]);
    }
  };

  /**
   * Right-click cancels lasso
   */
  const handleContextMenu = (e: React.MouseEvent) => {
    if (activeTool === "crop-lasso" && cropMode === "lasso") {
      e.preventDefault();
      setLassoPoints([]);
    }
  };

  // ======================================================================
  // Render
  // ======================================================================

  return (
    <div
    // style={{
    //   flex: 1,
    //   position: "relative",
    //   overflow: "hidden",
    //   background: "#1a1a1a",
    // }}
    // ref={containerRef}
    // onMouseDown={handleMouseDown}
    // onMouseMove={handleMouseMove}
    // onMouseUp={handleMouseUp}
    // onMouseLeave={() => {
    //   imageCanvas.endPan();
    //   setIsInteracting(false);
    // }}
    // onDoubleClick={handleDoubleClick}
    // onContextMenu={handleContextMenu}
    >
      {/* Hidden source canvas - stores actual pixel data (attached to canvasRef) */}
      {/* <canvas
        ref={canvasRef}
        style={{ display: "none" }}
        // style={{ display: "block", width: "100%", height: "100%" }}
      /> */}

      {/* Visible display canvas - renders transformed view */}
      <canvas
        ref={canvasRef}
        width={canvasRef.current?.width}
        height={canvasRef.current?.height}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={() => {
          imageCanvas.endPan();
          setIsInteracting(false);
        }}
        onDoubleClick={handleDoubleClick}
        onContextMenu={handleContextMenu}
        style={{ display: "block", width: "100%", height: "100%" }}
      />

      {/* Lasso instruction hint */}
      {activeTool === "crop-lasso" &&
        cropMode === "lasso" &&
        lassoPoints.length > 0 && (
          <div
            style={{
              position: "absolute",
              bottom: 20,
              left: "50%",
              transform: "translateX(-50%)",
              background: "rgba(0, 0, 0, 0.7)",
              color: "white",
              padding: "8px 16px",
              borderRadius: 4,
              fontSize: 12,
              pointerEvents: "none",
            }}
          >
            Double-click to finish • Right-click to cancel • Points:{" "}
            {lassoPoints.length}
          </div>
        )}
    </div>
  );
}

/**
 * Draw pixel grid overlay
 * Shows grid lines for precise pixel editing when zoomed in
 */
function drawPixelGrid(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  opacity: number,
) {
  ctx.save();
  ctx.globalAlpha = opacity;
  ctx.strokeStyle = "rgba(100, 150, 255, 0.5)";
  ctx.lineWidth = 1;

  // Vertical lines
  for (let x = 0; x <= width; x++) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
    ctx.stroke();
  }

  // Horizontal lines
  for (let y = 0; y <= height; y++) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
    ctx.stroke();
  }

  ctx.restore();
}
