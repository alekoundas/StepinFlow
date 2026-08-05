/**
 * OptionsPanel — right hand sidebar with view options, tool options and the
 * pending crop actions.
 */

import { Button } from "primereact/button";
import { InputSwitch } from "primereact/inputswitch";
import { Slider } from "primereact/slider";
import type {
  EditorTool,
  GridOptions,
  PendingSelection,
} from "@/windows/image-editor/types";
import { MAX_SCALE, MIN_SCALE } from "@/windows/image-editor/hooks/useViewTransform";

interface OptionsPanelProps {
  tool: EditorTool;
  scale: number;
  onScaleChange: (scale: number) => void;
  grid: GridOptions;
  onGridChange: (grid: GridOptions) => void;
  showMinimap: boolean;
  onShowMinimapChange: (value: boolean) => void;
  brushSize: number;
  onBrushSizeChange: (value: number) => void;
  selection: PendingSelection | null;
  onApplySelection: () => void;
  onClearSelection: () => void;
}

/** The zoom slider is logarithmic so the low end stays usable. */
const MIN_EXPONENT = Math.log2(MIN_SCALE);
const MAX_EXPONENT = Math.log2(MAX_SCALE);

export default function OptionsPanel({
  tool,
  scale,
  onScaleChange,
  grid,
  onGridChange,
  showMinimap,
  onShowMinimapChange,
  brushSize,
  onBrushSizeChange,
  selection,
  onApplySelection,
  onClearSelection,
}: OptionsPanelProps) {
  const isCropTool = tool.startsWith("crop-");
  const selectionReady = selection?.committed === true;

  return (
    <div className="image-editor__options">
      {isCropTool && (
        <section className="image-editor__section">
          <div className="image-editor__panel-title">Selection</div>

          {!selection && (
            <p className="image-editor__hint">
              {tool === "crop-rect" && "Drag on the image to select an area."}
              {tool === "crop-lasso" && "Drag to draw a freehand shape."}
              {tool === "crop-polygon" &&
                "Click to add points, double-click (or click the first point) to close."}
            </p>
          )}

          {selection?.kind === "rect" && (
            <p className="image-editor__hint">
              {selection.rect.width} x {selection.rect.height} px at{" "}
              {selection.rect.x}, {selection.rect.y}
            </p>
          )}

          {selection?.kind === "polygon" && (
            <p className="image-editor__hint">
              {selection.points.length} points
              {selection.committed ? "" : " (still drawing)"}
            </p>
          )}

          <div className="flex gap-2">
            <Button
              label="Apply crop"
              icon="pi pi-crop"
              size="small"
              className="flex-1"
              disabled={!selectionReady}
              onClick={onApplySelection}
              tooltip="Enter"
              tooltipOptions={{ position: "left" }}
            />
            <Button
              label="Clear"
              size="small"
              severity="secondary"
              outlined
              disabled={!selection}
              onClick={onClearSelection}
              tooltip="Esc"
              tooltipOptions={{ position: "left" }}
            />
          </div>
        </section>
      )}

      {tool === "eraser" && (
        <section className="image-editor__section">
          <div className="image-editor__panel-title">Eraser</div>
          <label className="image-editor__label">
            Brush size <span>{brushSize} px</span>
          </label>
          <Slider
            value={brushSize}
            min={1}
            max={200}
            step={1}
            onChange={(event) => onBrushSizeChange(event.value as number)}
          />
          <p className="image-editor__hint">
            Erased pixels become fully transparent in the exported PNG.
          </p>
        </section>
      )}

      <section className="image-editor__section">
        <div className="image-editor__panel-title">View</div>

        <label className="image-editor__label">
          Zoom <span>{Math.round(scale * 100)}%</span>
        </label>
        <Slider
          value={Math.log2(scale)}
          min={MIN_EXPONENT}
          max={MAX_EXPONENT}
          step={0.05}
          onChange={(event) => onScaleChange(2 ** (event.value as number))}
        />

        <div className="image-editor__switch-row">
          <span>Pixel grid</span>
          <InputSwitch
            checked={grid.enabled}
            onChange={(event) =>
              onGridChange({ ...grid, enabled: Boolean(event.value) })
            }
          />
        </div>

        <label className="image-editor__label">
          Grid opacity <span>{Math.round(grid.opacity * 100)}%</span>
        </label>
        <Slider
          value={grid.opacity}
          min={0.05}
          max={1}
          step={0.05}
          disabled={!grid.enabled}
          onChange={(event) =>
            onGridChange({ ...grid, opacity: event.value as number })
          }
        />
        <p className="image-editor__hint">
          The grid appears once one pixel is at least {grid.minScale}x zoom.
        </p>

        <div className="image-editor__switch-row">
          <span>Minimap</span>
          <InputSwitch
            checked={showMinimap}
            onChange={(event) => onShowMinimapChange(Boolean(event.value))}
          />
        </div>
      </section>

      <section className="image-editor__section">
        <div className="image-editor__panel-title">Shortcuts</div>
        <ul className="image-editor__shortcuts">
          <li>
            <b>Wheel</b> zoom at cursor
          </li>
          <li>
            <b>Space</b> / middle drag pan
          </li>
          <li>
            <b>Enter</b> apply crop · <b>Esc</b> clear
          </li>
          <li>
            <b>Ctrl+Z</b> undo · <b>Ctrl+Y</b> redo
          </li>
        </ul>
      </section>
    </div>
  );
}
