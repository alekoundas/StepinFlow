import type { Rectangle } from "electron";

interface Props {
  logicalRect: Rectangle;
  physicalRect: Rectangle;
  onConfirm: () => void;
  onRedraw: () => void;
  onCancel: () => void;
}

export default function OverlayConfirmBar({
  logicalRect,
  physicalRect,
  onConfirm,
  onRedraw,
  onCancel,
}: Props) {
  const barHeight = 44;
  const barWidth = 320;
  const MARGIN = 12;

  const viewportHeight = window.innerHeight;
  const belowY = logicalRect.y + logicalRect.height + MARGIN;
  const aboveY = logicalRect.y - barHeight - MARGIN;
  const top =
    belowY + barHeight < viewportHeight ? belowY : Math.max(aboveY, MARGIN);

  const centreX = logicalRect.x + logicalRect.width / 2 - barWidth / 2;
  const left = Math.max(
    MARGIN,
    Math.min(centreX, window.innerWidth - barWidth - MARGIN),
  );

  function btnStyle(variant: "primary" | "ghost"): React.CSSProperties {
    const base: React.CSSProperties = {
      padding: "4px 12px",
      borderRadius: 6,
      fontSize: 12,
      cursor: "pointer",
      border: "0.5px solid",
      background: "transparent",
    };
    return variant === "primary"
      ? {
          ...base,
          borderColor: "#60a5fa",
          color: "#93c5fd",
          background: "rgba(96,165,250,0.15)",
        }
      : {
          ...base,
          borderColor: "rgba(255,255,255,0.2)",
          color: "rgba(255,255,255,0.5)",
        };
  }

  return (
    <div
      style={{
        position: "absolute",
        top,
        left,
        width: barWidth,
        height: barHeight,
        background: "rgba(15,23,42,0.92)",
        border: "0.5px solid rgba(96,165,250,0.4)",
        borderRadius: 10,
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "0 14px",
        boxSizing: "border-box",
        pointerEvents: "all",
      }}
      onMouseDown={(e) => e.stopPropagation()}
    >
      <span
        style={{
          color: "rgba(255,255,255,0.5)",
          fontSize: 12,
          fontFamily: "monospace",
          flex: 1,
        }}
      >
        {physicalRect.width} × {physicalRect.height} px
      </span>
      <button
        onClick={onRedraw}
        style={btnStyle("ghost")}
      >
        Redraw
      </button>
      <button
        onClick={onConfirm}
        style={btnStyle("primary")}
      >
        Confirm
      </button>
      <button
        onClick={onCancel}
        style={btnStyle("ghost")}
      >
        Cancel
      </button>
      <span style={{ color: "rgba(255,255,255,0.25)", fontSize: 11 }}>ESC</span>
    </div>
  );
}
