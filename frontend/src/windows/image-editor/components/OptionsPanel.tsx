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

const TITLE_CLASS =
  "flex align-items-center justify-content-between mb-2 text-xs font-semibold uppercase text-color-secondary";
const TITLE_STYLE: React.CSSProperties = { letterSpacing: "0.06em" };

const LABEL_CLASS =
  "flex align-items-center justify-content-between mt-3 mb-2 text-sm";
const SWITCH_ROW_CLASS =
  "flex align-items-center justify-content-between mt-3 text-sm";
const HINT_CLASS = "my-2 text-xs line-height-3 text-color-secondary";
const VALUE_STYLE: React.CSSProperties = { fontVariantNumeric: "tabular-nums" };

/** Sections are separated by a rule, except whichever one comes first. */
function sectionClass(withDivider: boolean): string {
  return withDivider ? "mt-4 pt-3 border-top-1 surface-border" : "";
}

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
  const isEraser = tool === "eraser";
  const selectionReady = selection?.committed === true;

  return (
    <div className="flex-none p-3 overflow-y-auto">
      {isCropTool && (
        <section className={sectionClass(false)}>
          <div className={TITLE_CLASS} style={TITLE_STYLE}>
            Selection
          </div>

          {!selection && (
            <p className={HINT_CLASS}>
              {tool === "crop-rect" && "Drag on the image to select an area."}
              {tool === "crop-lasso" && "Drag to draw a freehand shape."}
              {tool === "crop-polygon" &&
                "Click to add points, double-click (or click the first point) to close."}
            </p>
          )}

          {selection?.kind === "rect" && (
            <p className={HINT_CLASS}>
              {selection.rect.width} x {selection.rect.height} px at{" "}
              {selection.rect.x}, {selection.rect.y}
            </p>
          )}

          {selection?.kind === "polygon" && (
            <p className={HINT_CLASS}>
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

      {isEraser && (
        <section className={sectionClass(false)}>
          <div className={TITLE_CLASS} style={TITLE_STYLE}>
            Eraser
          </div>
          <label className={LABEL_CLASS}>
            Brush size
            <span className="text-color-secondary" style={VALUE_STYLE}>
              {brushSize} px
            </span>
          </label>
          <Slider
            value={brushSize}
            min={1}
            max={200}
            step={1}
            onChange={(event) => onBrushSizeChange(event.value as number)}
          />
          <p className={HINT_CLASS}>
            Erased pixels become fully transparent in the exported PNG.
          </p>
        </section>
      )}

      <section className={sectionClass(isCropTool || isEraser)}>
        <div className={TITLE_CLASS} style={TITLE_STYLE}>
          View
        </div>

        <label className={LABEL_CLASS}>
          Zoom
          <span className="text-color-secondary" style={VALUE_STYLE}>
            {Math.round(scale * 100)}%
          </span>
        </label>
        <Slider
          value={Math.log2(scale)}
          min={MIN_EXPONENT}
          max={MAX_EXPONENT}
          step={0.05}
          onChange={(event) => onScaleChange(2 ** (event.value as number))}
        />

        <div className={SWITCH_ROW_CLASS}>
          <span>Pixel grid</span>
          <InputSwitch
            checked={grid.enabled}
            onChange={(event) =>
              onGridChange({ ...grid, enabled: Boolean(event.value) })
            }
          />
        </div>

        <label className={LABEL_CLASS}>
          Grid opacity
          <span className="text-color-secondary" style={VALUE_STYLE}>
            {Math.round(grid.opacity * 100)}%
          </span>
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
        <p className={HINT_CLASS}>
          The grid appears once one pixel is at least {grid.minScale}x zoom.
        </p>

        <div className={SWITCH_ROW_CLASS}>
          <span>Minimap</span>
          <InputSwitch
            checked={showMinimap}
            onChange={(event) => onShowMinimapChange(Boolean(event.value))}
          />
        </div>
      </section>

      <section className={sectionClass(true)}>
        <div className={TITLE_CLASS} style={TITLE_STYLE}>
          Shortcuts
        </div>
        <ul className="m-0 pl-3 text-xs line-height-3 text-color-secondary">
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
