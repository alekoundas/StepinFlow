import { useRef, useState } from "react";
import { Button } from "primereact/button";
import { Slider } from "primereact/slider";

import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  imageBase64: string;
  width: number;
  height: number;
  onConfirm: (x: number, y: number) => void;
  onCancel: () => void;
}

/**
 * Click point picker. Shares the editor window but not its canvas: the job is one click on a
 * small image, so a zoomable img with a crosshair is all it needs.
 *
 * The point is returned in template pixels, which is what the matcher scales at run time.
 */
export function ImagePointPickerComponent({
  imageBase64,
  width,
  height,
  onConfirm,
  onCancel,
}: Props) {
  const [zoom, setZoom] = useState(4);
  const [point, setPoint] = useState<{ x: number; y: number }>({
    x: Math.floor(width / 2),
    y: Math.floor(height / 2),
  });
  const [hover, setHover] = useState<{ x: number; y: number } | null>(null);

  const imageRef = useRef<HTMLImageElement>(null);

  const toImagePoint = (clientX: number, clientY: number) => {
    const rect = imageRef.current?.getBoundingClientRect();
    if (!rect) return null;

    return {
      x: Math.min(width - 1, Math.max(0, Math.floor(((clientX - rect.left) / rect.width) * width))),
      y: Math.min(height - 1, Math.max(0, Math.floor(((clientY - rect.top) / rect.height) * height))),
    };
  };

  return (
    <div className="flex flex-column h-full gap-3 p-3">
      <div className="flex align-items-center gap-3">
        <LabelComponent
          text="Click where the cursor should go"
          weight="bold"
        />
        <LabelComponent
          size="sm"
          color="secondary"
          text={`${point.x}, ${point.y}${hover ? `   (cursor ${hover.x}, ${hover.y})` : ""}`}
        />

        <div className="flex align-items-center gap-2 ml-auto" style={{ width: 220 }}>
          <LabelComponent
            size="sm"
            text={`${zoom}x`}
          />
          <Slider
            value={zoom}
            min={1}
            max={16}
            step={1}
            className="flex-1"
            onChange={(e) => setZoom(e.value as number)}
          />
        </div>
      </div>

      <div className="flex-1 overflow-auto surface-ground border-round p-3">
        <div
          style={{ position: "relative", width: width * zoom, height: height * zoom }}
        >
          <img
            ref={imageRef}
            src={`data:image/png;base64,${imageBase64}`}
            alt="template"
            draggable={false}
            style={{
              width: width * zoom,
              height: height * zoom,
              imageRendering: "pixelated",
              cursor: "crosshair",
              display: "block",
            }}
            onClick={(e) => {
              const next = toImagePoint(e.clientX, e.clientY);
              if (next) setPoint(next);
            }}
            onMouseMove={(e) => setHover(toImagePoint(e.clientX, e.clientY))}
            onMouseLeave={() => setHover(null)}
          />

          {/* Crosshair sits on the centre of the chosen pixel. */}
          <div
            style={{
              position: "absolute",
              left: (point.x + 0.5) * zoom,
              top: 0,
              bottom: 0,
              width: 1,
              backgroundColor: "var(--primary-color)",
              pointerEvents: "none",
            }}
          />
          <div
            style={{
              position: "absolute",
              top: (point.y + 0.5) * zoom,
              left: 0,
              right: 0,
              height: 1,
              backgroundColor: "var(--primary-color)",
              pointerEvents: "none",
            }}
          />
        </div>
      </div>

      <div className="flex justify-content-end gap-3">
        <Button
          label="Cancel"
          severity="secondary"
          onClick={onCancel}
        />
        <Button
          label="Use this point"
          icon="pi pi-check"
          onClick={() => onConfirm(point.x, point.y)}
        />
      </div>
    </div>
  );
}
