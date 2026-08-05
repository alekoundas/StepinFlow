/**
 * Image editor window.
 *
 * Opened by Electron with a PNG screenshot; the user crops / erases it and the
 * result is returned to the caller as PNG base64 (this is the template image an
 * IMAGE_SEARCH flow step will search for).
 *
 * Flow:
 *  1. signalReady()        -> base64 PNG of the image to edit
 *  2. user edits           -> every edit is a new undo/redo snapshot
 *  3. signalCloseWindow(b64 | null) -> Electron closes the window and resolves
 */

import { useCallback, useEffect, useRef, useState } from "react";
import { ProgressSpinner } from "primereact/progressspinner";
import { Message } from "primereact/message";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import Canvas from "@/windows/image-editor/components/Canvas";
import HistoryPanel from "@/windows/image-editor/components/HistoryPanel";
import Minimap from "@/windows/image-editor/components/Minimap";
import OptionsPanel from "@/windows/image-editor/components/OptionsPanel";
import Toolbar from "@/windows/image-editor/components/Toolbar";
import ToolRail from "@/windows/image-editor/components/ToolRail";
import { useImageCanvas } from "@/windows/image-editor/hooks/useImageCanvas";
import { useViewTransform } from "@/windows/image-editor/hooks/useViewTransform";
import type {
  EditorTool,
  GridOptions,
  PendingSelection,
  Point,
  Size,
} from "@/windows/image-editor/types";
import {
  canvasToPngBase64,
  loadImage,
} from "@/windows/image-editor/utils/canvas-utils";
import "@/windows/image-editor/ImageEditorPage.css";

const DEFAULT_GRID: GridOptions = { enabled: true, opacity: 0.25, minScale: 8 };
const ZOOM_STEP = 1.25;

export default function ImageEditorPage() {
  const image = useImageCanvas();
  const view = useViewTransform();

  const [tool, setTool] = useState<EditorTool>("crop-rect");
  const [grid, setGrid] = useState<GridOptions>(DEFAULT_GRID);
  const [showMinimap, setShowMinimap] = useState(true);
  const [brushSize, setBrushSize] = useState(16);
  const [selection, setSelection] = useState<PendingSelection | null>(null);
  const [cursor, setCursor] = useState<Point | null>(null);
  const [viewportSize, setViewportSize] = useState<Size>({
    width: 0,
    height: 0,
  });
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [saving, setSaving] = useState(false);

  const fittedRef = useRef(false);

  const { size: imageSize } = image;

  // ==========================================================================
  // Load the image handed over by Electron
  // ==========================================================================

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const base64 = await ElectronApiService.imageEditor.signalReady();
        if (cancelled) return;
        if (!base64) throw new Error("No image was passed to the editor");

        const bitmap = await loadImage(base64);
        if (cancelled) return;

        image.loadImage(bitmap);
        setStatus("ready");
      } catch (error: unknown) {
        if (cancelled) return;
        console.error("[ImageEditor] Failed to load image:", error);
        setErrorMessage(
          error instanceof Error ? error.message : "Failed to load image",
        );
        setStatus("error");
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
    // Runs once: the image is handed over exactly once per window.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Fit the image into the viewport as soon as both sizes are known.
  useEffect(() => {
    if (fittedRef.current) return;
    if (!imageSize || !viewportSize.width || !viewportSize.height) return;

    view.fit(imageSize, viewportSize);
    fittedRef.current = true;
  }, [imageSize, view, viewportSize]);

  // ==========================================================================
  // Actions
  // ==========================================================================

  const applySelection = useCallback(() => {
    if (!selection?.committed) return;

    if (selection.kind === "rect") {
      image.cropRect(selection.rect);
    } else {
      image.cropPolygon(selection.points);
    }

    setSelection(null);
    fittedRef.current = false; // re-fit the cropped result
  }, [image, selection]);

  const clearSelection = useCallback(() => setSelection(null), []);

  const zoomAtCenter = useCallback(
    (factor: number) => {
      view.zoomBy(factor, {
        x: viewportSize.width / 2,
        y: viewportSize.height / 2,
      });
    },
    [view, viewportSize.height, viewportSize.width],
  );

  const zoomFit = useCallback(() => {
    if (imageSize) view.fit(imageSize, viewportSize);
  }, [imageSize, view, viewportSize]);

  const zoomActual = useCallback(() => {
    if (imageSize) view.actualSize(imageSize, viewportSize);
  }, [imageSize, view, viewportSize]);

  const setScale = useCallback(
    (scale: number) => view.zoomToCenter(scale, viewportSize),
    [view, viewportSize],
  );

  const handleSave = useCallback(async () => {
    const canvas = image.getDocument();
    if (!canvas || saving) return;

    setSaving(true);
    try {
      const base64 = await canvasToPngBase64(canvas);
      ElectronApiService.imageEditor.signalCloseWindow(base64);
    } catch (error) {
      console.error("[ImageEditor] Failed to export PNG:", error);
      setErrorMessage("Failed to export the image");
      setSaving(false);
    }
  }, [image, saving]);

  const handleCancel = useCallback(() => {
    ElectronApiService.imageEditor.signalCloseWindow(null);
  }, []);

  // ==========================================================================
  // Keyboard shortcuts
  // ==========================================================================

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return;

      if (event.ctrlKey || event.metaKey) {
        switch (event.key.toLowerCase()) {
          case "z":
            event.preventDefault();
            if (event.shiftKey) image.redo();
            else image.undo();
            return;
          case "y":
            event.preventDefault();
            image.redo();
            return;
          case "s":
            event.preventDefault();
            void handleSave();
            return;
          default:
            return;
        }
      }

      switch (event.key) {
        case "Enter":
          applySelection();
          break;
        case "Escape":
          clearSelection();
          break;
        case "+":
        case "=":
          zoomAtCenter(ZOOM_STEP);
          break;
        case "-":
        case "_":
          zoomAtCenter(1 / ZOOM_STEP);
          break;
        case "0":
          zoomFit();
          break;
        case "1":
          zoomActual();
          break;
        case "h":
        case "H":
          setTool("hand");
          break;
        case "r":
        case "R":
          setTool("crop-rect");
          break;
        case "l":
        case "L":
          setTool("crop-lasso");
          break;
        case "p":
        case "P":
          setTool("crop-polygon");
          break;
        case "e":
        case "E":
          setTool("eraser");
          break;
        default:
          break;
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [
    applySelection,
    clearSelection,
    handleSave,
    image,
    zoomActual,
    zoomAtCenter,
    zoomFit,
  ]);

  // Switching tools drops a half-drawn selection.
  const handleToolChange = useCallback((next: EditorTool) => {
    setTool(next);
    setSelection(null);
  }, []);

  const handleViewportResize = useCallback((size: Size) => {
    setViewportSize(size);
  }, []);

  // ==========================================================================
  // Render
  // ==========================================================================

  return (
    <div className="image-editor">
      <Toolbar
        imageSize={imageSize}
        cursor={cursor}
        scale={view.scale}
        canUndo={image.history.canUndo}
        canRedo={image.history.canRedo}
        saving={saving}
        onUndo={image.undo}
        onRedo={image.redo}
        onZoomIn={() => zoomAtCenter(ZOOM_STEP)}
        onZoomOut={() => zoomAtCenter(1 / ZOOM_STEP)}
        onZoomFit={zoomFit}
        onZoomActual={zoomActual}
        onSave={handleSave}
        onCancel={handleCancel}
      />

      <div className="image-editor__body">
        <ToolRail tool={tool} onToolChange={handleToolChange} />

        <div className="image-editor__stage">
          <Canvas
            getDocument={image.getDocument}
            imageSize={imageSize}
            revision={image.revision}
            view={view}
            tool={tool}
            grid={grid}
            brushSize={brushSize}
            selection={selection}
            onSelectionChange={setSelection}
            onEraseSegment={image.eraseSegment}
            onEraseEnd={image.endErase}
            onCursorMove={setCursor}
            onViewportResize={handleViewportResize}
          />

          {status === "loading" && (
            <div className="image-editor__overlay-message">
              <ProgressSpinner style={{ width: 48, height: 48 }} />
              <span>Loading screenshot...</span>
            </div>
          )}

          {status === "error" && (
            <div className="image-editor__overlay-message">
              <Message severity="error" text={errorMessage} />
            </div>
          )}

          {showMinimap && status === "ready" && (
            <Minimap
              getDocument={image.getDocument}
              imageSize={imageSize}
              revision={image.revision}
              view={view}
              viewportSize={viewportSize}
            />
          )}
        </div>

        <aside className="image-editor__sidebar">
          <OptionsPanel
            tool={tool}
            scale={view.scale}
            onScaleChange={setScale}
            grid={grid}
            onGridChange={setGrid}
            showMinimap={showMinimap}
            onShowMinimapChange={setShowMinimap}
            brushSize={brushSize}
            onBrushSizeChange={setBrushSize}
            selection={selection}
            onApplySelection={applySelection}
            onClearSelection={clearSelection}
          />

          <HistoryPanel
            entries={image.history.entries}
            currentIndex={image.history.index}
            onSelect={image.jumpToHistory}
          />
        </aside>
      </div>
    </div>
  );
}
