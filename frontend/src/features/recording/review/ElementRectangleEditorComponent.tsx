import { useRef } from "react";
import type { CSSProperties, PointerEvent } from "react";

import type { ElementRectangle } from "@/features/recording/review/review";

// Annotations over a screenshot of any application, so fixed colours rather than the theme's.
const TEMPLATE_COLOUR = "#4F63E6";
const CLICK_COLOUR = "#E5484D";

// Smaller than this and nothing could be found by it.
const MINIMUM_SIDE = 4;

const CORNERS = ["nw", "ne", "sw", "se"] as const;
type Corner = (typeof CORNERS)[number];
type Handle = Corner | "move";

interface Props {
  imageSrc: string;

  // The screenshot's own pixels, which the rectangle and the click are in.
  width: number;
  height: number;

  click: { x: number; y: number } | null;
  element: ElementRectangle | null;
  isEditable: boolean;
  onChange: (element: ElementRectangle) => void;
}

/**
 * The main area at an action, with where the tester clicked and the rectangle the template will be
 * cut from. The rectangle's corners are dragged to change it, and its middle to move it, in the
 * screenshot's own pixels however small it is drawn.
 */
export default function ElementRectangleEditorComponent({
  imageSrc,
  width,
  height,
  click,
  element,
  isEditable,
  onChange,
}: Props) {
  const frameRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<{ handle: Handle; startX: number; startY: number; start: ElementRectangle } | null>(null);

  const begin = (handle: Handle) => (event: PointerEvent<HTMLElement>) => {
    if (!isEditable || !element) return;

    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.setPointerCapture(event.pointerId);
    dragRef.current = { handle, startX: event.clientX, startY: event.clientY, start: element };
  };

  const drag = (event: PointerEvent<HTMLElement>) => {
    const dragging = dragRef.current;
    const frame = frameRef.current;
    if (!dragging || !frame) return;

    const scale = width / frame.clientWidth;
    const dx = Math.round((event.clientX - dragging.startX) * scale);
    const dy = Math.round((event.clientY - dragging.startY) * scale);

    onChange(resize(dragging.start, dragging.handle, dx, dy, width, height));
  };

  const end = () => {
    dragRef.current = null;
  };

  return (
    <div
      ref={frameRef}
      className="relative w-full overflow-hidden border-round-lg select-none"
    >
      <img
        src={imageSrc}
        alt="The window at this action"
        className="w-full block"
        draggable={false}
      />

      {element && (
        <div
          aria-hidden="true"
          className="absolute"
          style={{
            ...placed(element.x, element.y, width, height),
            width: percent(element.width, width),
            height: percent(element.height, height),
            border: `2px dashed ${TEMPLATE_COLOUR}`,
            background: "rgba(79, 99, 230, 0.08)",
            cursor: isEditable ? "move" : undefined,
            touchAction: "none",
          }}
          onPointerDown={begin("move")}
          onPointerMove={drag}
          onPointerUp={end}
        >
          {isEditable &&
            CORNERS.map((corner) => (
              <span
                key={corner}
                className="absolute"
                style={{ ...cornerPlace(corner), width: 10, height: 10, background: TEMPLATE_COLOUR, borderRadius: 2 }}
                onPointerDown={begin(corner)}
                onPointerMove={drag}
                onPointerUp={end}
              />
            ))}
        </div>
      )}

      {click && (
        <span
          aria-hidden="true"
          className="absolute border-circle"
          style={{
            ...placed(click.x, click.y, width, height),
            width: 28,
            height: 28,
            marginLeft: -14,
            marginTop: -14,
            border: `2px solid ${CLICK_COLOUR}`,
            pointerEvents: "none",
          }}
        />
      )}
    </div>
  );
}

function percent(value: number, of: number): string {
  return `${(value * 100) / of}%`;
}

function placed(x: number, y: number, width: number, height: number): CSSProperties {
  return { left: percent(x, width), top: percent(y, height) };
}

function cornerPlace(corner: Corner): CSSProperties {
  const place: CSSProperties = {};
  if (corner.includes("n")) place.top = -6;
  else place.bottom = -6;
  if (corner.includes("w")) place.left = -6;
  else place.right = -6;

  place.cursor = corner === "nw" || corner === "se" ? "nwse-resize" : "nesw-resize";
  return place;
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.min(maximum, Math.max(minimum, value));
}

function resize(start: ElementRectangle, handle: Handle, dx: number, dy: number, width: number, height: number): ElementRectangle {
  if (handle === "move")
    return {
      x: clamp(start.x + dx, 0, width - start.width),
      y: clamp(start.y + dy, 0, height - start.height),
      width: start.width,
      height: start.height,
    };

  let left = start.x;
  let top = start.y;
  let right = start.x + start.width;
  let bottom = start.y + start.height;

  if (handle.includes("w")) left = clamp(left + dx, 0, right - MINIMUM_SIDE);
  if (handle.includes("e")) right = clamp(right + dx, left + MINIMUM_SIDE, width);
  if (handle.includes("n")) top = clamp(top + dy, 0, bottom - MINIMUM_SIDE);
  if (handle.includes("s")) bottom = clamp(bottom + dy, top + MINIMUM_SIDE, height);

  return { x: left, y: top, width: right - left, height: bottom - top };
}
