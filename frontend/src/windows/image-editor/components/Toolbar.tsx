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
  const toolButtonStyle = (isActive: boolean) => ({
    padding: "6px 12px",
    margin: "0 2px",
    backgroundColor: isActive ? "#669bff" : "#2a2a2a",
    color: "#ffffff",
    border: "1px solid #444",
    borderRadius: "4px",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: isActive ? "600" : "500",
    transition: "all 0.2s ease",
  });

  const cropModeButtonStyle = (isActive: boolean) => ({
    padding: "4px 8px",
    margin: "0 1px",
    backgroundColor: isActive ? "#669bff" : "#1a1a1a",
    color: "#ffffff",
    border: "1px solid #444",
    borderRadius: "3px",
    cursor: "pointer",
    fontSize: "11px",
    transition: "all 0.2s ease",
  });

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "8px",
        padding: "8px 12px",
        backgroundColor: "#2a2a2a",
        borderBottom: "1px solid #444",
        minHeight: "40px",
      }}
    >
      {/* ========== Tool Selection ========== */}
      <div style={{ display: "flex", gap: "4px", alignItems: "center" }}>
        <button
          style={toolButtonStyle(activeTool === "select")}
          onClick={() => onToolChange("select")}
          title="Pan & Select (Spacebar)"
        >
          👆 Select
        </button>

        <button
          style={toolButtonStyle(activeTool === "crop-rect" || activeTool === "crop-lasso")}
          onClick={() => onToolChange("crop-rect")}
          title="Rectangular Crop"
        >
          📐 Crop
        </button>

        {/* Crop Mode Selector (only show when crop is active) */}
        {(activeTool === "crop-rect" || activeTool === "crop-lasso") && (
          <div style={{ display: "flex", gap: "2px", marginLeft: "4px" }}>
            <button
              style={cropModeButtonStyle(cropMode === "rect")}
              onClick={() => onCropModeChange("rect")}
              title="Rectangular Crop"
            >
              Rect
            </button>
            <button
              style={cropModeButtonStyle(cropMode === "lasso")}
              onClick={() => onCropModeChange("lasso")}
              title="Freehand Lasso Crop"
            >
              Lasso
            </button>
          </div>
        )}

        <button
          style={toolButtonStyle(activeTool === "eraser")}
          onClick={() => onToolChange("eraser")}
          title="Eraser - Make pixels transparent"
        >
          🧹 Erase
        </button>
      </div>

      {/* ========== Separator ========== */}
      <div style={{ width: "1px", height: "24px", backgroundColor: "#444", margin: "0 4px" }}></div>

      {/* ========== Undo/Redo ========== */}
      <div style={{ display: "flex", gap: "4px", alignItems: "center" }}>
        <button
          style={{
            ...toolButtonStyle(false),
            opacity: canUndo ? 1 : 0.5,
            cursor: canUndo ? "pointer" : "not-allowed",
          }}
          onClick={onUndo}
          disabled={!canUndo}
          title="Undo (Ctrl+Z)"
        >
          ↶ Undo
        </button>
        <button
          style={{
            ...toolButtonStyle(false),
            opacity: canRedo ? 1 : 0.5,
            cursor: canRedo ? "pointer" : "not-allowed",
          }}
          onClick={onRedo}
          disabled={!canRedo}
          title="Redo (Ctrl+Y)"
        >
          ↷ Redo
        </button>
      </div>

      {/* ========== Zoom Controls ========== */}
      <div style={{ display: "flex", gap: "4px", alignItems: "center" }}>
        <button
          style={toolButtonStyle(false)}
          onClick={() => onZoomChange(zoomLevel - 0.2)}
          title="Zoom Out"
        >
          🔍−
        </button>
        <span
          style={{
            minWidth: "50px",
            textAlign: "center",
            fontSize: "12px",
            color: "#aaa",
          }}
        >
          {Math.round(zoomLevel * 100)}%
        </span>
        <button
          style={toolButtonStyle(false)}
          onClick={() => onZoomChange(zoomLevel + 0.2)}
          title="Zoom In"
        >
          🔍+
        </button>
      </div>

      {/* ========== Separator ========== */}
      <div style={{ width: "1px", height: "24px", backgroundColor: "#444", margin: "0 4px" }}></div>

      {/* ========== Grid Controls ========== */}
      <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
        <label
          style={{
            display: "flex",
            alignItems: "center",
            gap: "6px",
            cursor: "pointer",
            fontSize: "12px",
            color: "#aaa",
          }}
        >
          <input
            type="checkbox"
            checked={showGrid}
            onChange={(e) => onShowGridChange(e.target.checked)}
            style={{ cursor: "pointer" }}
          />
          <span title="Show pixel grid (visible when zoomed in)">Grid</span>
        </label>

        {showGrid && (
          <input
            type="range"
            min="0"
            max="1"
            step="0.1"
            value={gridOpacity}
            onChange={(e) => onGridOpacityChange(parseFloat(e.target.value))}
            title="Grid opacity"
            style={{ width: "80px" }}
          />
        )}
      </div>

      {/* ========== Minimap Toggle ========== */}
      <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
        <label
          style={{
            display: "flex",
            alignItems: "center",
            gap: "6px",
            cursor: "pointer",
            fontSize: "12px",
            color: "#aaa",
          }}
        >
          <input
            type="checkbox"
            checked={showMinimap}
            onChange={(e) => onShowMinimapChange(e.target.checked)}
            style={{ cursor: "pointer" }}
          />
          <span title="Show minimap for navigation">Minimap</span>
        </label>
      </div>

      {/* ========== Spacer ========== */}
      <div style={{ flex: 1 }}></div>

      {/* ========== Export/Cancel ========== */}
      <div style={{ display: "flex", gap: "4px", alignItems: "center" }}>
        <button
          style={{
            ...toolButtonStyle(false),
            backgroundColor: "#dc3545",
          }}
          onClick={onCancel}
          title="Cancel editing"
        >
          ✕ Cancel
        </button>
        <button
          style={{
            ...toolButtonStyle(false),
            backgroundColor: "#28a745",
          }}
          onClick={onExport}
          title="Export image"
        >
          ✓ Export
        </button>
      </div>
    </div>
  );
}
