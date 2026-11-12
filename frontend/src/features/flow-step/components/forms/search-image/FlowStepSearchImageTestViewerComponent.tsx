import { useEffect, useRef, useState } from "react";
import type { PointerEvent, WheelEvent } from "react";
import { Button } from "primereact/button";

import type { ImageSearchTestMatchDto } from "@/shared/models/database/image-search-test-result-dto";

interface Props {
  screenshot: string;
  width: number;
  height: number;
  hits: ImageSearchTestMatchDto[];
  nearMiss: ImageSearchTestMatchDto | null;
  refused: ImageSearchTestMatchDto[];
}

const ZOOMS = [1, 1.5, 2, 3, 4, 6, 8];
const HIT_COLOUR = "var(--green-400)";
const NEAR_MISS_COLOUR = "var(--orange-400)";
const REFUSED_COLOUR = "var(--red-400)";

const percent = (value: number, total: number) => (total > 0 ? `${(value / total) * 100}%` : "0%");

/**
 * The screenshot the search was made on, with the selected template's hits, the shapes its colour
 * refused and its closest place under the bar. 1 is the whole area fitted to the width; a box
 * clicked is zoomed to, and dragging pans. Ctrl and the wheel zoom too.
 */
export function FlowStepSearchImageTestViewerComponent({ screenshot, width, height, hits, nearMiss, refused }: Props) {
  const viewport = useRef<HTMLDivElement>(null);
  const drag = useRef<{ x: number; y: number; left: number; top: number } | null>(null);
  const [zoom, setZoom] = useState(1);

  // Where to centre once the zoom has laid out: a fraction of the screenshot, or keep the view.
  const [centre, setCentre] = useState<{ x: number; y: number } | null>(null);

  useEffect(() => {
    const view = viewport.current;
    if (!view || centre === null) return;

    view.scrollLeft = centre.x * view.scrollWidth - view.clientWidth / 2;
    view.scrollTop = centre.y * view.scrollHeight - view.clientHeight / 2;
  }, [centre, zoom]);

  const zoomTo = (next: number, at?: { x: number; y: number }) => {
    const view = viewport.current;
    const middle = view
      ? {
          x: (view.scrollLeft + view.clientWidth / 2) / view.scrollWidth,
          y: (view.scrollTop + view.clientHeight / 2) / view.scrollHeight,
        }
      : { x: 0.5, y: 0.5 };

    setZoom(next);
    setCentre(at ?? middle);
  };

  const step = (direction: 1 | -1) => {
    const index = ZOOMS.indexOf(zoom);
    const next = ZOOMS[Math.min(ZOOMS.length - 1, Math.max(0, index + direction))];
    zoomTo(next);
  };

  const focus = (match: ImageSearchTestMatchDto) => {
    const at = { x: (match.x + match.width / 2) / width, y: (match.y + match.height / 2) / height };
    zoomTo(Math.max(zoom, 3), at);
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    const view = viewport.current;
    if (!view || zoom === 1) return;

    drag.current = { x: e.clientX, y: e.clientY, left: view.scrollLeft, top: view.scrollTop };
    view.setPointerCapture(e.pointerId);
  };

  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    const view = viewport.current;
    if (!view || !drag.current) return;

    view.scrollLeft = drag.current.left - (e.clientX - drag.current.x);
    view.scrollTop = drag.current.top - (e.clientY - drag.current.y);
  };

  const onWheel = (e: WheelEvent<HTMLDivElement>) => {
    if (!e.ctrlKey) return;
    step(e.deltaY < 0 ? 1 : -1);
  };

  return (
    <div className="flex flex-column gap-2 flex-1" style={{ minHeight: 0 }}>
      <div className="flex align-items-center gap-2">
        <span className="text-xs text-color-secondary flex-1">
          Click a box to zoom to it, drag to look around, Ctrl and the wheel to zoom.
        </span>
        <Button type="button" icon="pi pi-search-minus" size="small" text aria-label="Zoom out" disabled={zoom === ZOOMS[0]} onClick={() => step(-1)} />
        <span className="text-xs" style={{ minWidth: "3rem", textAlign: "center" }}>{`${Math.round(zoom * 100)}%`}</span>
        <Button type="button" icon="pi pi-search-plus" size="small" text aria-label="Zoom in" disabled={zoom === ZOOMS[ZOOMS.length - 1]} onClick={() => step(1)} />
        <Button type="button" label="Fit" size="small" text onClick={() => zoomTo(1, { x: 0.5, y: 0.5 })} />
      </div>

      <div
        ref={viewport}
        className="overflow-auto border-round surface-ground flex-1"
        style={{ minHeight: "14rem", cursor: zoom > 1 ? "grab" : "default" }}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={() => {
          drag.current = null;
        }}
        onWheel={onWheel}
      >
        {/* Boxes are in percentages of the screenshot, so they zoom with it. */}
        <div className="relative" style={{ width: `${zoom * 100}%` }}>
          <img
            src={`data:image/jpeg;base64,${screenshot}`}
            alt=""
            className="w-full h-auto block"
            draggable={false}
          />

          {nearMiss && (
            <Box match={nearMiss} width={width} height={height} colour={NEAR_MISS_COLOUR} line="dashed" onClick={() => focus(nearMiss)} />
          )}
          {refused.map((x, index) => (
            <Box key={`refused-${index}`} match={x} width={width} height={height} colour={REFUSED_COLOUR} line="dotted" onClick={() => focus(x)} />
          ))}
          {hits.map((x, index) => (
            <Box key={index} match={x} width={width} height={height} colour={HIT_COLOUR} line="solid" onClick={() => focus(x)} />
          ))}
        </div>
      </div>

      <div className="flex flex-wrap gap-4 text-xs text-color-secondary">
        <span className="flex align-items-center gap-2">
          <span style={{ width: 14, height: 10, border: `2px solid ${HIT_COLOUR}` }} />
          hit, with where it clicks
        </span>
        {refused.length > 0 && (
          <span className="flex align-items-center gap-2">
            <span style={{ width: 14, height: 10, border: `2px dotted ${REFUSED_COLOUR}` }} />
            the shape, refused for its colour or contrast
          </span>
        )}
        <span className="flex align-items-center gap-2">
          <span style={{ width: 14, height: 10, border: `2px dashed ${NEAR_MISS_COLOUR}` }} />
          closest place under the bar
        </span>
      </div>
    </div>
  );
}

interface BoxProps {
  match: ImageSearchTestMatchDto;
  width: number;
  height: number;
  colour: string;
  // Solid for a hit, dotted for a shape its colour refused, dashed for the closest under the bar.
  line: "solid" | "dotted" | "dashed";
  onClick: () => void;
}

function Box({ match, width, height, colour, line, onClick }: BoxProps) {
  const refusal = match.refusedBy ? ` · ${match.refusedBy.toLowerCase()}` : "";

  return (
    <div
      className="absolute cursor-pointer"
      onPointerDown={(e) => e.stopPropagation()}
      onClick={onClick}
      title={`${match.score.toFixed(2)}${refusal}${match.scale !== 1 ? ` · scaled ${match.scale.toFixed(2)}x` : ""}`}
      style={{
        left: percent(match.x, width),
        top: percent(match.y, height),
        width: percent(match.width, width),
        height: percent(match.height, height),
        border: `2px ${line} ${colour}`,
        boxSizing: "border-box",
      }}
    >
      <span
        className="absolute text-xs px-1 border-round-sm white-space-nowrap"
        style={{ top: -20, left: -2, background: colour, color: "var(--gray-900)" }}
      >
        {`${match.score.toFixed(2)}${refusal}`}
      </span>

      {/* Where the cursor lands once the click offset is applied. A box in the right place with a
          dot in the wrong one is its own bug. */}
      {line === "solid" && (
        <div
          className="absolute border-circle"
          style={{
            left: percent(match.clickX - match.x, match.width),
            top: percent(match.clickY - match.y, match.height),
            width: 8,
            height: 8,
            marginLeft: -4,
            marginTop: -4,
            background: colour,
            boxShadow: "0 0 0 2px rgba(0,0,0,0.5)",
          }}
        />
      )}
    </div>
  );
}
