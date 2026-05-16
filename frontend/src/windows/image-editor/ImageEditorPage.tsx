import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { Divider } from "primereact/divider";
import { ToggleButton } from "primereact/togglebutton";
import { Slider } from "primereact/slider";
import { ScrollPanel } from "primereact/scrollpanel";
import { Toolbar } from "primereact/toolbar";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import {
  base64ToUint8Array,
  uint8ArrayToDataURL,
} from "@/windows/image-editor/utils/canvas-utils";

type Tool = "hand" | "crop-rect" | "lasso" | "eraser";

interface HistoryState {
  id: string;
  imageData: ImageData;
  name: string;
  timestamp: Date;
}

export default function ImageEditorPage() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [imageData, setImageData] = useState<ImageData | null>(null);
  const [scale, setScale] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const [lastMouse, setLastMouse] = useState({ x: 0, y: 0 });

  const [tool, setTool] = useState<Tool>("hand");
  const [showGrid, setShowGrid] = useState(true);
  const [gridOpacity, setGridOpacity] = useState(0.25);

  const [history, setHistory] = useState<HistoryState[]>([]);
  const [historyIndex, setHistoryIndex] = useState(-1);

  const [selection, setSelection] = useState<{
    x: number;
    y: number;
    w: number;
    h: number;
  } | null>(null);
  const [isSelecting, setIsSelecting] = useState(false);

  // Load image from Electron
  useEffect(() => {
    ElectronApiService.imageEditor.signalReady().then((receivedData: any) => {
      if (!receivedData) return;

      const data =
        typeof receivedData === "string"
          ? base64ToUint8Array(receivedData)
          : receivedData;

      const dataUrl = uint8ArrayToDataURL(data);
      const img = new Image();
      img.onload = () => initializeCanvas(img);
      img.src = dataUrl;
    });
  }, []);

  const initializeCanvas = (img: HTMLImageElement) => {
    const canvas = canvasRef.current!;
    canvas.width = img.width;
    canvas.height = img.height;

    const ctx = canvas.getContext("2d", { willReadFrequently: true })!;
    ctx.imageSmoothingEnabled = false;
    ctx.drawImage(img, 0, 0);

    const initialData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    setImageData(initialData);
    saveToHistory("Original", initialData);
  };

  const getContext = () =>
    canvasRef.current!.getContext("2d", { willReadFrequently: true })!;

  const saveToHistory = (name: string, data: ImageData) => {
    const newState: HistoryState = {
      id: crypto.randomUUID(),
      imageData: data,
      name,
      timestamp: new Date(),
    };

    setHistory((prev) => {
      const trimmed = prev.slice(0, historyIndex + 1);
      return [...trimmed, newState];
    });
    setHistoryIndex((prev) => Math.min(prev + 1, history.length));
  };

  const restoreFromHistory = (index: number) => {
    if (index < 0 || index >= history.length) return;
    const state = history[index];
    const ctx = getContext();
    ctx.putImageData(state.imageData, 0, 0);
    setImageData(state.imageData);
    setHistoryIndex(index);
  };

  // ============== COORDINATE TRANSFORMATION ==============
  const getCanvasMousePos = (e: React.MouseEvent) => {
    const rect = canvasRef.current!.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    // Convert screen → canvas logical coordinates
    const canvasX = Math.floor((mouseX - offset.x) / scale);
    const canvasY = Math.floor((mouseY - offset.y) / scale);

    return {
      x: Math.max(0, Math.min(canvasX, canvasRef.current!.width - 1)),
      y: Math.max(0, Math.min(canvasY, canvasRef.current!.height - 1)),
    };
  };

  // ============== MOUSE HANDLERS ==============
  const handleMouseDown = (e: React.MouseEvent) => {
    if (e.button !== 0) return;
    const pos = getCanvasMousePos(e);

    if (tool === "hand") {
      setIsDragging(true);
      setLastMouse({ x: e.clientX, y: e.clientY });
    } else if (tool === "crop-rect") {
      setIsSelecting(true);
      setSelection({ x: pos.x, y: pos.y, w: 0, h: 0 });
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    const pos = getCanvasMousePos(e);

    if (isDragging && tool === "hand") {
      const dx = e.clientX - lastMouse.x;
      const dy = e.clientY - lastMouse.y;

      setOffset((prev) => ({
        x: prev.x + dx,
        y: prev.y + dy,
      }));

      setLastMouse({ x: e.clientX, y: e.clientY });
    }

    if (isSelecting && selection) {
      const currentPos = getCanvasMousePos(e);
      setSelection({
        x: Math.min(selection.x, currentPos.x),
        y: Math.min(selection.y, currentPos.y),
        w: Math.abs(currentPos.x - selection.x),
        h: Math.abs(currentPos.y - selection.y),
      });
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    if (isSelecting && selection && selection.w > 5 && selection.h > 5) {
      applyCrop();
    }
    setIsSelecting(false);
  };

  // ============== CROP ==============
  const applyCrop = () => {
    if (!selection) return;

    const { x, y, w, h } = selection;
    const ctx = getContext();
    const cropped = ctx.getImageData(x, y, w, h);

    const canvas = canvasRef.current!;
    canvas.width = w;
    canvas.height = h;
    ctx.putImageData(cropped, 0, 0);

    const newImageData = ctx.getImageData(0, 0, w, h);
    setImageData(newImageData);
    saveToHistory("Cropped", newImageData);

    setSelection(null);
    setOffset({ x: 0, y: 0 }); // Reset view
    setScale(1);
  };

  // ============== RENDERING ==============
  const redraw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas || !imageData) return;

    const ctx = getContext();
    ctx.imageSmoothingEnabled = false;

    // Clear and redraw with current transform
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.putImageData(imageData, 0, 0);
  }, [imageData]);

  useEffect(() => {
    redraw();
  }, [redraw]);

  // Apply CSS transform to canvas (this is the key!)
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    canvas.style.transformOrigin = "0 0";
    canvas.style.transform = `translate(${offset.x}px, ${offset.y}px) scale(${scale})`;
  }, [scale, offset]);

  return (
    <div className="flex flex-column h-screen bg-gray-950">
      <Toolbar
        className="bg-gray-900 border-b border-gray-700"
        start={
          <div className="flex gap-2">
            <Button
              icon="pi pi-save"
              label="Save Template"
              severity="success"
            />
            <Button
              icon="pi pi-undo"
              onClick={() => restoreFromHistory(historyIndex - 1)}
              disabled={historyIndex <= 0}
            />
            <Button
              icon="pi pi-redo"
              onClick={() => restoreFromHistory(historyIndex + 1)}
              disabled={historyIndex >= history.length - 1}
            />
          </div>
        }
        end={
          <div className="flex items-center gap-4 text-sm">
            <span>Zoom: {Math.round(scale * 100)}%</span>
          </div>
        }
      />

      <div className="flex flex-1 overflow-hidden">
        {/* Tools */}
        <div className="w-16 bg-gray-900 border-r border-gray-700 flex flex-col items-center py-4 gap-3">
          {[
            { tool: "hand", icon: "pi pi-hand", label: "Pan" },
            { tool: "crop-rect", icon: "pi pi-crop", label: "Rect Crop" },
            { tool: "eraser", icon: "pi pi-eraser", label: "Eraser" },
          ].map((t) => (
            <Button
              key={t.tool}
              icon={t.icon}
              tooltip={t.label}
              tooltipOptions={{ position: "right" }}
              className={`w-12 h-12 ${tool === t.tool ? "bg-blue-600" : "bg-gray-800"}`}
              onClick={() => setTool(t.tool as Tool)}
            />
          ))}
          <Divider className="my-2" />
          <ToggleButton
            checked={showGrid}
            onChange={(e) => setShowGrid(e.value)}
            onIcon="pi pi-table"
            offIcon="pi pi-table"
          />
        </div>

        {/* Canvas Area */}
        <div
          ref={containerRef}
          className="flex-1 overflow-auto bg-[radial-gradient(#333_1px,transparent_1px)] bg-[length:20px_20px] flex items-center justify-center relative"
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseUp}
        >
          <canvas
            ref={canvasRef}
            className="shadow-2xl"
            onMouseDown={handleMouseDown}
            style={{
              imageRendering: "pixelated",
              cursor: tool === "hand" ? "grab" : "crosshair",
            }}
          />

          {/* Selection Overlay */}
          {selection && (
            <div
              className="absolute border-2 border-blue-500 pointer-events-none"
              style={{
                left: selection.x * scale + offset.x,
                top: selection.y * scale + offset.y,
                width: selection.w * scale,
                height: selection.h * scale,
              }}
            />
          )}
        </div>

        {/* Right Sidebar */}
        <div className="w-80 bg-gray-900 border-l border-gray-700 p-4 flex flex-col gap-4">
          <div>
            <label>Zoom</label>
            <Slider
              value={scale}
              onChange={(e) => setScale(e.value as number)}
              min={0.1}
              max={8}
              step={0.05}
            />
          </div>

          <div>
            <label>Grid Opacity</label>
            <Slider
              value={gridOpacity}
              onChange={(e) => setGridOpacity(e.value as number)}
              min={0}
              max={1}
              step={0.05}
            />
          </div>

          <Button
            label="Reset View"
            icon="pi pi-refresh"
            onClick={() => {
              setScale(1);
              setOffset({ x: 0, y: 0 });
            }}
          />
        </div>
      </div>
    </div>
  );
}
