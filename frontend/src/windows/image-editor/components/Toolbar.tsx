/**
 * Toolbar — top bar: save/cancel, undo/redo, zoom controls and the readouts
 * (image size, cursor position, zoom level).
 */

import { Button } from "primereact/button";
import type { Point, Size } from "@/windows/image-editor/types";

interface ToolbarProps {
  imageSize: Size | null;
  cursor: Point | null;
  scale: number;
  canUndo: boolean;
  canRedo: boolean;
  saving: boolean;
  onUndo: () => void;
  onRedo: () => void;
  onZoomIn: () => void;
  onZoomOut: () => void;
  onZoomFit: () => void;
  onZoomActual: () => void;
  onSave: () => void;
  onCancel: () => void;
}

export default function Toolbar({
  imageSize,
  cursor,
  scale,
  canUndo,
  canRedo,
  saving,
  onUndo,
  onRedo,
  onZoomIn,
  onZoomOut,
  onZoomFit,
  onZoomActual,
  onSave,
  onCancel,
}: ToolbarProps) {
  return (
    <div className="image-editor__toolbar">
      <div className="flex align-items-center gap-2">
        <Button
          icon="pi pi-check"
          label="Use image"
          severity="success"
          size="small"
          loading={saving}
          onClick={onSave}
          tooltip="Save and return this image (Ctrl+S)"
          tooltipOptions={{ position: "bottom" }}
        />
        <Button
          icon="pi pi-times"
          label="Cancel"
          severity="secondary"
          outlined
          size="small"
          onClick={onCancel}
        />

        <span className="image-editor__divider" />

        <Button
          icon="pi pi-undo"
          size="small"
          text
          disabled={!canUndo}
          onClick={onUndo}
          tooltip="Undo (Ctrl+Z)"
          tooltipOptions={{ position: "bottom" }}
        />
        <Button
          icon="pi pi-refresh"
          size="small"
          text
          disabled={!canRedo}
          onClick={onRedo}
          tooltip="Redo (Ctrl+Y)"
          tooltipOptions={{ position: "bottom" }}
        />
      </div>

      <div className="flex align-items-center gap-2">
        <Button
          icon="pi pi-search-minus"
          size="small"
          text
          onClick={onZoomOut}
          tooltip="Zoom out (-)"
          tooltipOptions={{ position: "bottom" }}
        />
        <span className="image-editor__zoom-value">
          {formatZoom(scale)}
        </span>
        <Button
          icon="pi pi-search-plus"
          size="small"
          text
          onClick={onZoomIn}
          tooltip="Zoom in (+)"
          tooltipOptions={{ position: "bottom" }}
        />
        <Button
          label="Fit"
          size="small"
          text
          onClick={onZoomFit}
          tooltip="Fit to window (0)"
          tooltipOptions={{ position: "bottom" }}
        />
        <Button
          label="1:1"
          size="small"
          text
          onClick={onZoomActual}
          tooltip="Actual size (1)"
          tooltipOptions={{ position: "bottom" }}
        />

        <span className="image-editor__divider" />

        <span className="image-editor__readout">
          {imageSize
            ? `${imageSize.width} x ${imageSize.height} px`
            : "loading..."}
        </span>
        <span className="image-editor__readout image-editor__readout--mono">
          {cursor ? `X ${cursor.x}  Y ${cursor.y}` : "X -  Y -"}
        </span>
      </div>
    </div>
  );
}

function formatZoom(scale: number): string {
  if (scale >= 1) return `${Math.round(scale * 100)}%`;
  return `${(scale * 100).toFixed(scale < 0.1 ? 1 : 0)}%`;
}
