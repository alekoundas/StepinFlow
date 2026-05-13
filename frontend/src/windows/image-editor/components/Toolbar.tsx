/**
 * Toolbar Component - Top toolbar with controls
 *
 * Features:
 *  - Tool selection buttons
 *  - Zoom controls
 *  - Undo/Redo buttons
 *  - Grid toggle & opacity
 *  - Minimap toggle
 *  - Export/Cancel buttons
 */

interface ToolbarProps {
  activeTool: "crop-rect" | "crop-lasso" | "eraser" | "select";
  onToolChange: (
    tool: "crop-rect" | "crop-lasso" | "eraser" | "select",
  ) => void;
  cropMode: "rect" | "lasso";
  onCropModeChange: (mode: "rect" | "lasso") => void;
  onExport: () => void;
  onCancel: () => void;
  canUndo: boolean;
  canRedo: boolean;
  onUndo: () => void;
  onRedo: () => void;
  zoomLevel: number;
  onZoomChange: (zoom: number) => void;
  showGrid: boolean;
  onShowGridChange: (show: boolean) => void;
  gridOpacity: number;
  onGridOpacityChange: (opacity: number) => void;
  showMinimap: boolean;
  onShowMinimapChange: (show: boolean) => void;
}

export default function Toolbar({
  activeTool,
  onToolChange,
  cropMode,
  onCropModeChange,
  onExport,
  onCancel,
  canUndo,
  canRedo,
  onUndo,
  onRedo,
  zoomLevel,
  onZoomChange,
  showGrid,
  onShowGridChange,
  gridOpacity,
  onGridOpacityChange,
  showMinimap,
  onShowMinimapChange,
}: ToolbarProps) {
  const toolButtonClass = (tool: string) =>
    `toolbar-btn ${activeTool === tool ? "active" : ""}`;
  const cropModeButtonClass = (mode: string) =>
    `crop-mode-btn ${cropMode === mode ? "active" : ""}`;

  return (
    <div className="editor-toolbar">
      {/* ========== Tool Selection ========== */}
      <div className="toolbar-group">
        <button
          className={toolButtonClass("select")}
          onClick={() => onToolChange("select")}
          title="Pan & Select (Spacebar)"
        >
          👆 Select
        </button>

        <button
          className={toolButtonClass("crop-rect")}
          onClick={() => onToolChange("crop-rect")}
          title="Rectangular Crop"
        >
          📐 Crop
        </button>

        {/* Crop Mode Selector (only show when crop is active) */}
        {(activeTool === "crop-rect" || activeTool === "crop-lasso") && (
          <div className="crop-mode-selector">
            <button
              className={cropModeButtonClass("rect")}
              onClick={() => onCropModeChange("rect")}
              title="Rectangular Crop"
            >
              Rect
            </button>
            <button
              className={cropModeButtonClass("lasso")}
              onClick={() => onCropModeChange("lasso")}
              title="Freehand Lasso Crop"
            >
              Lasso
            </button>
          </div>
        )}

        <button
          className={toolButtonClass("eraser")}
          onClick={() => onToolChange("eraser")}
          title="Eraser - Make pixels transparent"
        >
          🧹 Erase
        </button>
      </div>

      {/* ========== Separtor ========== */}
      <div className="toolbar-divider"></div>

      {/* ========== Undo/Redo ========== */}
      <div className="toolbar-group">
        <button
          className="toolbar-btn"
          onClick={onUndo}
          disabled={!canUndo}
          title="Undo (Ctrl+Z)"
        >
          ↶ Undo
        </button>
        <button
          className="toolbar-btn"
          onClick={onRedo}
          disabled={!canRedo}
          title="Redo (Ctrl+Y)"
        >
          ↷ Redo
        </button>
      </div>

      {/* ========== Zoom Controls ========== */}
      <div className="toolbar-group">
        <button
          className="toolbar-btn"
          onClick={() => onZoomChange(zoomLevel - 0.2)}
          title="Zoom Out"
        >
          🔍−
        </button>
        <span className="zoom-display">{Math.round(zoomLevel * 100)}%</span>
        <button
          className="toolbar-btn"
          onClick={() => onZoomChange(zoomLevel + 0.2)}
          title="Zoom In"
        >
          🔍+
        </button>
      </div>

      {/* ========== Separtor ========== */}
      <div className="toolbar-divider"></div>

      {/* ========== Grid Controls ========== */}
      <div className="toolbar-group">
        <label className="toolbar-checkbox">
          <input
            type="checkbox"
            checked={showGrid}
            onChange={(e) => onShowGridChange(e.target.checked)}
          />
          <span title="Show pixel grid (visible when zoomed in)">Grid</span>
        </label>

        {showGrid && (
          <div className="opacity-slider">
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={gridOpacity}
              onChange={(e) => onGridOpacityChange(parseFloat(e.target.value))}
              title="Grid opacity"
            />
          </div>
        )}
      </div>

      {/* ========== Minimap Toggle ========== */}
      <div className="toolbar-group">
        <label className="toolbar-checkbox">
          <input
            type="checkbox"
            checked={showMinimap}
            onChange={(e) => onShowMinimapChange(e.target.checked)}
          />
          <span title="Show minimap for navigation">Minimap</span>
        </label>
      </div>

      {/* ========== Spacer ========== */}
      <div style={{ flex: 1 }}></div>

      {/* ========== Export/Cancel ========== */}
      <div className="toolbar-group">
        <button
          className="toolbar-btn toolbar-cancel"
          onClick={onCancel}
        >
          ✕ Cancel
        </button>
        <button
          className="toolbar-btn toolbar-export"
          onClick={onExport}
        >
          ✓ Export
        </button>
      </div>
    </div>
  );
}
