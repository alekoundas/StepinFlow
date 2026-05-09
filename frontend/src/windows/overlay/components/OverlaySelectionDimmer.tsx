import type { Rectangle } from "electron";

interface Props {
  selectionRect: Rectangle | null;
}

export default function OverlaySelectionDimmer({ selectionRect }: Props) {
  const dim = "rgba(0,0,0,0.60)";
  return (
    <>
      {/*========   Display Fullscreen Dimmer    ========*/}
      {selectionRect === null && (
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: dim,
            pointerEvents: "none",
          }}
        />
      )}

      {/*========   Display Dimmer around the selection   ========*/}
      {selectionRect !== null && (
        <>
          <div
            style={{
              position: "absolute",
              top: 0,
              left: 0,
              right: 0,
              height: selectionRect.y,
              background: dim,
              pointerEvents: "none",
            }}
          />
          <div
            style={{
              position: "absolute",
              top: selectionRect.y + selectionRect.height,
              left: 0,
              right: 0,
              bottom: 0,
              background: dim,
              pointerEvents: "none",
            }}
          />
          <div
            style={{
              position: "absolute",
              top: selectionRect.y,
              left: 0,
              width: selectionRect.x,
              height: selectionRect.height,
              background: dim,
              pointerEvents: "none",
            }}
          />
          <div
            style={{
              position: "absolute",
              top: selectionRect.y,
              left: selectionRect.x + selectionRect.width,
              right: 0,
              height: selectionRect.height,
              background: dim,
              pointerEvents: "none",
            }}
          />
        </>
      )}
    </>
  );
}
