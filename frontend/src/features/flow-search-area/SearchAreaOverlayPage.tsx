/**
 * SearchAreaOverlayPage
 *
 * Route: /search-area-overlay
 *
 * This page is loaded inside the transparent fullscreen Electron overlay window.
 * It:
 *  1. Signals to main that it's ready (so main sends the screenshot)
 *  2. Renders the screenshot as a frozen desktop background
 *  3. Lets the user drag to select a rectangular region
 *  4. Shows a live W×H readout during drag
 *  5. After mouse-release shows a confirm/cancel bar
 *  6. Sends the result back via electronApi.searchArea.sendResult()
 */

import { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import React, {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";

interface Point {
  x: number;
  y: number;
}

interface AreaRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

type Phase = "idle" | "dragging" | "confirming";

// Normalise a rect so width/height are always positive
function normaliseRect(start: Point, end: Point): AreaRect {
  return {
    x: Math.round(Math.min(start.x, end.x)),
    y: Math.round(Math.min(start.y, end.y)),
    width: Math.round(Math.abs(end.x - start.x)),
    height: Math.round(Math.abs(end.y - start.y)),
  };
}

export default function SearchAreaOverlayPage() {
  const [screenshot, setScreenshot] = useState<string | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [startPoint, setStartPoint] = useState<Point | null>(null);
  const [currentPoint, setCurrentPoint] = useState<Point | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // ── Signal ready + subscribe to screenshot ─────────────────────────────────
  useLayoutEffect(() => {
    const api = ElectronApiService.searchArea;
    if (!api) return;

    api.signalReady();
    ElectronApiService.backendApi.System.takeScreenshot(
      new ScreenshotRequestDto({ isFullScreen: true }),
    )
      .then((screenshotBytes: Uint8Array | string) => {
        let base64: string;

        if (screenshotBytes instanceof Uint8Array) {
          base64 = Buffer.from(screenshotBytes).toString("base64");
        } else {
          base64 = screenshotBytes as string; // in case it's already base64
        }

        setScreenshot(`data:image/png;base64,${base64}`);
      })
      .catch((err) => {
        console.error("Failed to take screenshot:", err);
      });

    return;
  }, []);

  // ── ESC to cancel ──────────────────────────────────────────────────────────
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") sendResult(null);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  // ── Mouse handlers ─────────────────────────────────────────────────────────
  const onMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (e.button !== 0) return;
    const pt = { x: e.clientX, y: e.clientY };
    setStartPoint(pt);
    setCurrentPoint(pt);
    setPhase("dragging");
  }, []);

  const onMouseMove = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (phase !== "dragging") return;
      setCurrentPoint({ x: e.clientX, y: e.clientY });
    },
    [phase],
  );

  const onMouseUp = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (phase !== "dragging" || !startPoint) return;
      const end = { x: e.clientX, y: e.clientY };
      setCurrentPoint(end);
      const rect = normaliseRect(startPoint, end);
      // If selection is too small (accidental click), restart
      if (rect.width < 4 || rect.height < 4) {
        setPhase("idle");
        setStartPoint(null);
        setCurrentPoint(null);
        return;
      }
      setPhase("confirming");
    },
    [phase, startPoint],
  );

  const sendResult = useCallback((rect: AreaRect | null) => {
    ElectronApiService.searchArea?.sendResult(rect);
  }, []);

  const handleConfirm = useCallback(() => {
    if (!startPoint || !currentPoint) return;
    sendResult(normaliseRect(startPoint, currentPoint));
  }, [startPoint, currentPoint, sendResult]);

  const handleCancel = useCallback(() => {
    sendResult(null);
  }, [sendResult]);

  const handleRestart = useCallback(() => {
    setPhase("idle");
    setStartPoint(null);
    setCurrentPoint(null);
  }, []);

  // ── Derived geometry ───────────────────────────────────────────────────────
  const selectionRect =
    startPoint && currentPoint ? normaliseRect(startPoint, currentPoint) : null;

  const isDraggingOrConfirming = phase === "dragging" || phase === "confirming";

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div
      ref={containerRef}
      onMouseDown={onMouseDown}
      onMouseMove={onMouseMove}
      onMouseUp={onMouseUp}
      style={{
        position: "fixed",
        inset: 0,
        userSelect: "none",
        cursor: phase === "confirming" ? "default" : "crosshair",
        overflow: "hidden",
      }}
    >
      aaaa
      {/* ── Frozen desktop screenshot ──────────────────────────────────────── */}
      {screenshot && (
        <img
          src={screenshot}
          alt=""
          draggable={false}
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            objectFit: "fill",
            pointerEvents: "none",
          }}
        />
      )}
      {/* ── Full dim overlay ───────────────────────────────────────────────── */}
      {!isDraggingOrConfirming && (
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: "rgba(0,0,0,0.45)",
            pointerEvents: "none",
          }}
        />
      )}
      {/* ── Dim areas around selection (4 rects) ──────────────────────────── */}
      {isDraggingOrConfirming && selectionRect && (
        <DimMask rect={selectionRect} />
      )}
      {/* ── Selection border + handles ─────────────────────────────────────── */}
      {isDraggingOrConfirming && selectionRect && selectionRect.width > 0 && (
        <SelectionBox
          rect={selectionRect}
          phase={phase}
        />
      )}
      {/* ── Live W×H readout (during drag) ────────────────────────────────── */}
      {phase === "dragging" && selectionRect && (
        <ReadoutLabel rect={selectionRect} />
      )}
      {/* ── Confirm / Cancel bar (after release) ──────────────────────────── */}
      {phase === "confirming" && selectionRect && (
        <ConfirmBar
          rect={selectionRect}
          onConfirm={handleConfirm}
          onRestart={handleRestart}
          onCancel={handleCancel}
        />
      )}
      {/* ── Idle hint ─────────────────────────────────────────────────────── */}
      {phase === "idle" && <HintBanner />}
    </div>
  );
}

// ─── Sub-components ───────────────────────────────────────────────────────────

function DimMask({ rect }: { rect: AreaRect }) {
  const dim = "rgba(0,0,0,0.50)";
  return (
    <>
      {/* Top */}
      <div
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          height: rect.y,
          background: dim,
          pointerEvents: "none",
        }}
      />
      {/* Bottom */}
      <div
        style={{
          position: "absolute",
          top: rect.y + rect.height,
          left: 0,
          right: 0,
          bottom: 0,
          background: dim,
          pointerEvents: "none",
        }}
      />
      {/* Left */}
      <div
        style={{
          position: "absolute",
          top: rect.y,
          left: 0,
          width: rect.x,
          height: rect.height,
          background: dim,
          pointerEvents: "none",
        }}
      />
      {/* Right */}
      <div
        style={{
          position: "absolute",
          top: rect.y,
          left: rect.x + rect.width,
          right: 0,
          height: rect.height,
          background: dim,
          pointerEvents: "none",
        }}
      />
    </>
  );
}

function SelectionBox({ rect, phase }: { rect: AreaRect; phase: Phase }) {
  const handleSize = 8;
  const handleColor = "#60a5fa";

  const handles =
    phase === "confirming"
      ? [
          // corners
          { top: -handleSize / 2, left: -handleSize / 2 },
          { top: -handleSize / 2, right: -handleSize / 2 },
          { bottom: -handleSize / 2, left: -handleSize / 2 },
          { bottom: -handleSize / 2, right: -handleSize / 2 },
          // mid-edges
          { top: -handleSize / 2, left: "calc(50% - 4px)" },
          { bottom: -handleSize / 2, left: "calc(50% - 4px)" },
          { top: "calc(50% - 4px)", left: -handleSize / 2 },
          { top: "calc(50% - 4px)", right: -handleSize / 2 },
        ]
      : [];

  return (
    <div
      style={{
        position: "absolute",
        top: rect.y,
        left: rect.x,
        width: rect.width,
        height: rect.height,
        border: "2px solid #60a5fa",
        boxSizing: "border-box",
        pointerEvents: "none",
      }}
    >
      {handles.map((style, i) => (
        <div
          key={i}
          style={{
            position: "absolute",
            width: handleSize,
            height: handleSize,
            background: handleColor,
            borderRadius: 2,
            ...style,
          }}
        />
      ))}
    </div>
  );
}

function ReadoutLabel({ rect }: { rect: AreaRect }) {
  const OFFSET = 6;
  // Position above the selection; flip below if near top edge
  const above = rect.y - OFFSET - 26;
  const top = above > 0 ? above : rect.y + rect.height + OFFSET;

  return (
    <div
      style={{
        position: "absolute",
        top,
        left: rect.x,
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
      {rect.x}, {rect.y} &nbsp;·&nbsp; {rect.width} × {rect.height}
    </div>
  );
}

function ConfirmBar({
  rect,
  onConfirm,
  onRestart,
  onCancel,
}: {
  rect: AreaRect;
  onConfirm: () => void;
  onRestart: () => void;
  onCancel: () => void;
}) {
  const barHeight = 44;
  const barWidth = 320;
  const MARGIN = 12;

  // Try to position below the selection; flip above if near bottom
  const viewportHeight = window.innerHeight;
  const belowY = rect.y + rect.height + MARGIN;
  const aboveY = rect.y - barHeight - MARGIN;
  const top =
    belowY + barHeight < viewportHeight ? belowY : Math.max(aboveY, MARGIN);

  // Centre horizontally on the selection, clamped to viewport
  const centreX = rect.x + rect.width / 2 - barWidth / 2;
  const left = Math.max(
    MARGIN,
    Math.min(centreX, window.innerWidth - barWidth - MARGIN),
  );

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
        // allow clicks on the bar itself, block drag
        pointerEvents: "all",
      }}
      // Stop mousedown propagating so bar clicks don't restart the drag
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
        {rect.width} × {rect.height}
      </span>

      <button
        onClick={onRestart}
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

function HintBanner() {
  return (
    <div
      style={{
        position: "absolute",
        top: 16,
        left: "50%",
        transform: "translateX(-50%)",
        background: "rgba(15,23,42,0.75)",
        color: "rgba(255,255,255,0.6)",
        fontSize: 13,
        fontFamily: "sans-serif",
        padding: "6px 16px",
        borderRadius: 8,
        pointerEvents: "none",
        whiteSpace: "nowrap",
      }}
    >
      Click and drag to select a screen area &nbsp;·&nbsp; ESC to cancel
    </div>
  );
}

function btnStyle(variant: "primary" | "ghost"): React.CSSProperties {
  const base: React.CSSProperties = {
    padding: "4px 12px",
    borderRadius: 6,
    fontSize: 12,
    fontFamily: "sans-serif",
    cursor: "pointer",
    border: "0.5px solid",
    background: "transparent",
  };
  if (variant === "primary") {
    return {
      ...base,
      borderColor: "#60a5fa",
      color: "#93c5fd",
      background: "rgba(96,165,250,0.15)",
    };
  }
  return {
    ...base,
    borderColor: "rgba(255,255,255,0.2)",
    color: "rgba(255,255,255,0.5)",
  };
}
