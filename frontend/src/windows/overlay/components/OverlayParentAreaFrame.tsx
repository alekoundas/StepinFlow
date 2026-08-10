import type { Rectangle } from "electron";

interface Props {
  // The parent search area in this monitor's logical coords, or null when it does not reach
  // this monitor at all -- in which case nothing here is selectable.
  logicalRect: Rectangle | null;
  name: string;
}

/**
 * Shades the part of the screen the selection cannot reach and outlines the part it can.
 *
 * Sits under the selection dimmer, so out of bounds ends up dimmed twice and reads as darker
 * than merely unselected. The drag is clamped in OverlayCapturePage; this is what makes the
 * clamp look deliberate rather than broken.
 */
export default function OverlayParentAreaFrame({ logicalRect, name }: Props) {
  const outOfBounds = "rgba(0,0,0,0.45)";
  const accent = "#fbbf24";

  if (logicalRect === null) {
    return (
      <div
        style={{
          position: "absolute",
          inset: 0,
          background: outOfBounds,
          pointerEvents: "none",
        }}
      />
    );
  }

  const panel = {
    position: "absolute" as const,
    background: outOfBounds,
    pointerEvents: "none" as const,
  };

  return (
    <>
      {/*========   Shade everything outside the parent area   ========*/}
      <div style={{ ...panel, top: 0, left: 0, right: 0, height: logicalRect.y }} />
      <div
        style={{
          ...panel,
          top: logicalRect.y + logicalRect.height,
          left: 0,
          right: 0,
          bottom: 0,
        }}
      />
      <div
        style={{
          ...panel,
          top: logicalRect.y,
          left: 0,
          width: logicalRect.x,
          height: logicalRect.height,
        }}
      />
      <div
        style={{
          ...panel,
          top: logicalRect.y,
          left: logicalRect.x + logicalRect.width,
          right: 0,
          height: logicalRect.height,
        }}
      />

      {/*========   Outline the area the selection has to stay in   ========*/}
      <div
        style={{
          position: "absolute",
          top: logicalRect.y,
          left: logicalRect.x,
          width: logicalRect.width,
          height: logicalRect.height,
          border: `2px dashed ${accent}`,
          boxSizing: "border-box",
          pointerEvents: "none",
        }}
      />

      {/*========   Name it, so the boundary is not a mystery   ========*/}
      <div
        style={{
          position: "absolute",
          top: Math.max(0, logicalRect.y - 24),
          left: logicalRect.x,
          background: "rgba(15,23,42,0.88)",
          color: accent,
          fontFamily: "sans-serif",
          fontSize: 12,
          padding: "2px 8px",
          borderRadius: 4,
          pointerEvents: "none",
          whiteSpace: "nowrap",
        }}
      >
        {name}
      </div>
    </>
  );
}
