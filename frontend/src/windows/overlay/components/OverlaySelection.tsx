import type { Phase } from "@/windows/overlay/OverlayCapturePage";
import type { Rectangle } from "electron";

interface Props {
  logicalSelectionRect: Rectangle;
  physicalSelectionRect: Rectangle;
  phase: Phase;
  //   onEdit?: (id: number) => void;
  //   onClone?: (id: number) => void;
  //   onDelete?: (id: number) => void;
}

export default function OverlaySelection({
  logicalSelectionRect,
  physicalSelectionRect,
  phase,
}: Props) {
  const LABEL_OFFSET = 6;
  const above = logicalSelectionRect.y - LABEL_OFFSET - 26;
  const below =
    logicalSelectionRect.y + logicalSelectionRect.height + LABEL_OFFSET;
  const top = above > 0 ? above : below;

  return (
    <>
      {/*========   Display selected area   ========*/}
      <div
        style={{
          position: "absolute",
          top: logicalSelectionRect.y,
          left: logicalSelectionRect.x,
          width: logicalSelectionRect.width,
          height: logicalSelectionRect.height,
          border: "2px solid #60a5fa",
          boxSizing: "border-box",
          pointerEvents: "none",
        }}
      ></div>

      {/*========   Display dimensions   ========*/}
      {phase === "dragging" && (
        <div
          style={{
            position: "absolute",
            top: top,
            left: logicalSelectionRect.x,
            background: "rgba(15,23,42,0.88)",
            color: "#93c5fd",
            fontFamily: "monospace",
            fontSize: 12,
            padding: "3px 8px",
            borderRadius: 4,
            pointerEvents: "none",
            whiteSpace: "nowrap",
          }}
        >
          {/* Physical dimensions - what backend will use */}
          {physicalSelectionRect.width} × {physicalSelectionRect.height} px
        </div>
      )}
    </>
  );
}
