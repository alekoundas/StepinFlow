/**
 * ImageEditorPage - Main image editor window
 *
 * Features:
 *  - Zoom & Pan navigation
 *  - Rectangular & Freehand crop tools
 *  - Eraser (make pixels transparent)
 *  - Pixel grid (toggleable, with opacity control)
 *  - Minimap for navigation
 *  - Undo/Redo stack with thumbnails
 *  - Fast canvas-based rendering
 *
 * Architecture:
 *  - State: useImageCanvas + useUndoRedo hooks
 *  - Rendering: HTML5 Canvas API only (no external image libs)
 *  - Communication: Electron IPC (data URL in, PNG bytes out)
 */

import { useCallback, useEffect, useRef, useState } from "react";
import { useImageCanvas } from "./hooks/useImageCanvas";
import { useUndoRedo } from "./hooks/useUndoRedo";
import Canvas from "./components/Canvas";
import Toolbar from "./components/Toolbar";
import HistoryPanel from "./components/HistoryPanel";
import Minimap from "./components/Minimap";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import "./ImageEditorPage.css";
import { uint8ArrayToDataURL } from "@/windows/image-editor/utils/canvas-utils";

type EditorTool = "crop-rect" | "crop-lasso" | "eraser" | "select";
type CropMode = "rect" | "lasso";

export default function ImageEditorPage() {
  // ========================================================================
  // State
  // ========================================================================

  // Image & canvas state
  const [imageLoaded, setImageLoaded] = useState(false);
  const [stepId, setStepId] = useState<string | undefined>();
  const canvasRef = useRef<HTMLCanvasElement>(new HTMLCanvasElement());
  const containerRef = useRef<HTMLDivElement>(new HTMLDivElement());

  // Editor tool selection
  const [activeTool, setActiveTool] = useState<EditorTool>("select");
  const [cropMode, setCropMode] = useState<CropMode>("rect");

  // UI state
  const [showGrid, setShowGrid] = useState(true);
  const [gridOpacity, setGridOpacity] = useState(0.3);
  const [showMinimap, setShowMinimap] = useState(true);

  // Canvas manipulation (zoom, pan)
  const imageCanvas = useImageCanvas(canvasRef);

  // Undo/Redo management
  const history = useUndoRedo(imageCanvas.saveCanvasState);

  // ========================================================================
  // Initialization - Load image from Electron
  // ========================================================================

  useEffect(() => {
    // Signal that React page is ready to receive image
    ElectronApiService.imageEditor.signalReady().then((imageData) => {
      if (imageData && canvasRef.current) {
        const img = new Image();
        img.onload = () => {
          const canvas = canvasRef.current!;
          canvas.width = img.width;
          canvas.height = img.height;

          const ctx = canvas.getContext("2d");
          if (ctx) {
            ctx.drawImage(img, 0, 0);
            imageCanvas.resetZoomPan();
            history.reset();
            setImageLoaded(true);
            // setStepId(data.stepId);
          }
        };
        img.src = uint8ArrayToDataURL(imageData);
      }
    }) || (() => {});
  }, [imageCanvas, history]);

  // ========================================================================
  // Tool-specific callbacks
  // ========================================================================

  // Rectangular crop - from user interaction, crop the canvas
  const handleCropRectFinished = useCallback(
    (rect: { x: number; y: number; width: number; height: number }) => {
      if (!canvasRef.current) return;

      history.recordAction("Crop (Rectangle)");
      imageCanvas.cropRectangle(rect);
      setActiveTool("select");
    },
    [imageCanvas, history],
  );

  // Freehand/lasso crop
  const handleCropLassoFinished = useCallback(
    (points: Array<{ x: number; y: number }>) => {
      if (!canvasRef.current || points.length < 3) return;

      history.recordAction("Crop (Lasso)");
      imageCanvas.cropLasso(points);
      setActiveTool("select");
    },
    [imageCanvas, history],
  );

  // Eraser action - make pixels transparent
  const handleErasePixels = useCallback(
    (points: Array<{ x: number; y: number }>, brushSize: number) => {
      if (!canvasRef.current) return;

      history.recordAction("Erase");
      imageCanvas.erasePixels(points, brushSize);
    },
    [imageCanvas, history],
  );

  // Save to PNG bytes and send back to main window
  const handleExport = useCallback(() => {
    if (!canvasRef.current) return;

    canvasRef.current.toBlob((blob) => {
      if (!blob) return;

      const reader = new FileReader();
      reader.onload = () => {
        const pngBytes = new Uint8Array(reader.result as ArrayBuffer);

        // Send result back to Electron main process
        ElectronApiService.imageEditor.signalCloseWindow(pngBytes);

        // Close the window
        window.close();
      };
      reader.readAsArrayBuffer(blob);
    }, "image/png");
  }, [stepId]);

  const handleCancel = useCallback(() => {
    ElectronApiService.imageEditor.signalCloseWindow(null);
    window.close();
  }, [stepId]);

  // ========================================================================
  // Render
  // ========================================================================

  if (!imageLoaded) {
    return (
      <div className="editor-loading">
        <div className="spinner"></div>
        <p>Loading image...</p>
      </div>
    );
  }

  return (
    <div className="editor-container">
      {/* ================ Toolbar - Top ================ */}
      <Toolbar
        activeTool={activeTool}
        onToolChange={setActiveTool}
        cropMode={cropMode}
        onCropModeChange={setCropMode}
        onExport={handleExport}
        onCancel={handleCancel}
        canUndo={history.canUndo()}
        canRedo={history.canRedo()}
        onUndo={() => {
          history.undo();
          imageCanvas.restoreCanvasState(history.currentState!);
        }}
        onRedo={() => {
          history.redo();
          imageCanvas.restoreCanvasState(history.currentState!);
        }}
        zoomLevel={imageCanvas.zoom}
        onZoomChange={(z) => imageCanvas.setZoom(z)}
        showGrid={showGrid}
        onShowGridChange={setShowGrid}
        gridOpacity={gridOpacity}
        onGridOpacityChange={setGridOpacity}
        showMinimap={showMinimap}
        onShowMinimapChange={setShowMinimap}
      />

      {/* ================ Main Editor Area ================ */}
      <div
        className="editor-main"
        ref={containerRef}
      >
        {/* Canvas - Core image editing surface */}
        <Canvas
          canvasRef={canvasRef}
          containerRef={containerRef}
          activeTool={activeTool}
          cropMode={cropMode}
          showGrid={showGrid}
          gridOpacity={gridOpacity}
          imageCanvas={imageCanvas}
          onCropRectFinished={handleCropRectFinished}
          onCropLassoFinished={handleCropLassoFinished}
          onErasePixels={handleErasePixels}
        />

        {/* Minimap - Navigation preview */}
        {showMinimap && (
          <Minimap
            canvasRef={canvasRef}
            imageCanvas={imageCanvas}
            containerRef={containerRef}
          />
        )}
      </div>

      {/* ================ History Panel - Right Sidebar ================ */}
      <HistoryPanel
        history={history}
        onSelectHistoryItem={(index) => {
          history.jumpToIndex(index);
          imageCanvas.restoreCanvasState(history.currentState!);
        }}
      />
    </div>
  );
}
