/**
 * Route: /search-area-overlay
 *
 * Cross-monitor selection overlay.
 *
 * Coordinate contract:
 *  - All selection state (startPhys, endPhys) is in PHYSICAL ABSOLUTE coords
 *    (i.e. virtual desktop physical pixels, top-left of primary = 0,0)
 *  - Local CSS/logical coords are used ONLY at the edges (mouse input → broadcast,
 *    broadcast → render clip)
 *
 * Flow:
 *  1. signalReady()      → get screenshot + monitor metadata
 *  2. local mouse event  → convert to physical absolute → signalMouseEvent()
 *  3. broadcastMouseEvent → update shared state (all windows, including sender)
 *  4. render             → clip physical rect to this monitor → CSS coords
 *  5. confirm            → send physical absolute rect via signalCloseWindow()
 */

import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { Rectangle } from "electron";
import React, {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import type {
  SignalMouseEvent,
  SignalReadyResponse,
} from "../../../../electron/shared/types";

// ─── Types ────────────────────────────────────────────────────────────────────

interface Point {
  x: number;
  y: number;
}

interface PhysRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

type Phase = "idle" | "dragging" | "confirming";

// ─── Helpers ──────────────────────────────────────────────────────────────────

function normalisePoints(a: Point, b: Point): PhysRect {
  const x = Math.min(a.x, b.x);
  const y = Math.min(a.y, b.y);
  return {
    x,
    y,
    width: Math.abs(b.x - a.x),
    height: Math.abs(b.y - a.y),
  };
}

// ─── Component ────────────────────────────────────────────────────────────────

export default function SearchAreaOverlayPage() {
  const [screenshot, setScreenshot] = useState<string | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");

  // Selection stored in physical absolute coords
  const [startPhys, setStartPhys] = useState<Point | null>(null);
  const [endPhys, setEndPhys] = useState<Point | null>(null);

  // Monitor metadata — stable refs (set once on ready, never change)
  const scaleFactor = useRef(1);
  const monitorLogicalOrigin = useRef<Point>({ x: 0, y: 0 });
  const logicalSize = useRef({ width: 0, height: 0 });

  // ── Coordinate converters ──────────────────────────────────────────────────

  // CSS local (this window) → physical absolute (virtual desktop)
  const toPhysAbs = useCallback(
    (cssX: number, cssY: number): Point => ({
      x: Math.round(
        (monitorLogicalOrigin.current.x + cssX) * scaleFactor.current,
      ),
      y: Math.round(
        (monitorLogicalOrigin.current.y + cssY) * scaleFactor.current,
      ),
    }),
    [],
  );

  // Physical absolute → CSS local for THIS monitor's window
  // Returns null if the point is outside this monitor
  const physAbsToLocalCss = useCallback(
    (physX: number, physY: number): Point => ({
      x: physX / scaleFactor.current - monitorLogicalOrigin.current.x,
      y: physY / scaleFactor.current - monitorLogicalOrigin.current.y,
    }),
    [],
  );

  // Clip a physical absolute rect to this monitor and return local CSS rect.
  // Returns null if the selection doesn't overlap this monitor at all.
  const clipToLocalCss = useCallback((physRect: PhysRect): Rectangle | null => {
    const sf = scaleFactor.current;
    const origin = monitorLogicalOrigin.current;
    const size = logicalSize.current;

    // This monitor's physical bounds
    const monPhysX = origin.x * sf;
    const monPhysY = origin.y * sf;
    const monPhysRight = (origin.x + size.width) * sf;
    const monPhysBottom = (origin.y + size.height) * sf;

    // Clip
    const clipX = Math.max(physRect.x, monPhysX);
    const clipY = Math.max(physRect.y, monPhysY);
    const clipRight = Math.min(physRect.x + physRect.width, monPhysRight);
    const clipBottom = Math.min(physRect.y + physRect.height, monPhysBottom);

    if (clipRight <= clipX || clipBottom <= clipY) return null;

    // Convert clipped physical → local CSS
    return {
      x: Math.round(clipX / sf - origin.x),
      y: Math.round(clipY / sf - origin.y),
      width: Math.round((clipRight - clipX) / sf),
      height: Math.round((clipBottom - clipY) / sf),
    };
  }, []);

  // ── Signal ready ───────────────────────────────────────────────────────────

  useLayoutEffect(() => {
    ElectronApiService.searchArea
      .signalReady()
      .then((res: SignalReadyResponse | null) => {
        if (!res) return;

        scaleFactor.current = res.scaleFactor;
        monitorLogicalOrigin.current = res.monitorLogicalOrigin;
        logicalSize.current = {
          width: res.logicalWidth,
          height: res.logicalHeight,
        };

        const blob = base64ToBlob(res.screenshot.toString());
        setScreenshot(URL.createObjectURL(blob));
      })
      .catch((err) => console.error("[Overlay] signalReady failed:", err));
  }, []);

  // ── Broadcast listener — single source of truth for all state ─────────────
  //
  // Local mouse handlers only call signalMouseEvent().
  // This listener updates everyone (including the sender) so state is always
  // in sync across all monitor windows.

  useEffect(() => {
    const unsub = ElectronApiService.searchArea.broadcastMouseEvent(
      (event: SignalMouseEvent) => {
        const pt: Point = { x: event.physicalX, y: event.physicalY };
        console.log("yek: ", event);

        if (event.type === "down") {
          setStartPhys(pt);
          setEndPhys(pt);
          setPhase("dragging");
        } else if (event.type === "move") {
          setEndPhys(pt);
        } else if (event.type === "up") {
          setEndPhys(pt);
          // Too small = accidental click → back to idle
          if (startPhys) {
            const r = normalisePoints(startPhys, pt);
            if (r.width < 4 || r.height < 4) {
              setPhase("idle");
              setStartPhys(null);
              setEndPhys(null);
              return;
            }
          }
          setPhase("confirming");
        }
      },
    );
    return unsub;
  }, []);

  // ── ESC to cancel ──────────────────────────────────────────────────────────

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") sendResult(null);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  // ── Local mouse → broadcast (no state update here) ────────────────────────

  const onMouseDown = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (e.button !== 0) return;
      const phys = toPhysAbs(e.clientX, e.clientY);
      ElectronApiService.searchArea.signalMouseEvent({
        type: "down",
        physicalX: phys.x,
        physicalY: phys.y,
      });
    },
    [toPhysAbs],
  );

  const onMouseMove = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (phase !== "dragging") return;
      const phys = toPhysAbs(e.clientX, e.clientY);
      ElectronApiService.searchArea.signalMouseEvent({
        type: "move",
        physicalX: phys.x,
        physicalY: phys.y,
      });
    },
    [phase, toPhysAbs],
  );

  const onMouseUp = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (phase !== "dragging") return;
      const phys = toPhysAbs(e.clientX, e.clientY);
      ElectronApiService.searchArea.signalMouseEvent({
        type: "up",
        physicalX: phys.x,
        physicalY: phys.y,
      });
    },
    [phase, toPhysAbs],
  );

  // ── Result ─────────────────────────────────────────────────────────────────

  const sendResult = useCallback((rect: Rectangle | null) => {
    ElectronApiService.searchArea.signalCloseWindow(rect);
  }, []);

  const handleConfirm = useCallback(() => {
    if (!startPhys || !endPhys) return;
    // Send physical absolute rect — backend speaks physical
    sendResult(normalisePoints(startPhys, endPhys));
  }, [startPhys, endPhys, sendResult]);

  const handleCancel = useCallback(() => sendResult(null), [sendResult]);

  const handleRestart = useCallback(() => {
    setPhase("idle");
    setStartPhys(null);
    setEndPhys(null);
  }, []);

  // ── Derived geometry (local CSS, clipped to this monitor) ─────────────────

  const physRect =
    startPhys && endPhys ? normalisePoints(startPhys, endPhys) : null;

  const localCssRect = physRect ? clipToLocalCss(physRect) : null;

  const isDraggingOrConfirming = phase === "dragging" || phase === "confirming";

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div
      // onMouseDown={onMouseDown}
      // onMouseMove={onMouseMove}
      // onMouseUp={onMouseUp}
      style={{
        position: "fixed",
        inset: 0,
        userSelect: "none",
        cursor: phase === "confirming" ? "default" : "crosshair",
        overflow: "hidden",
        pointerEvents: phase === "confirming" ? "auto" : "none", // TODO test
      }}
    >
      {/* Frozen desktop screenshot */}
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

      {/* Full dim — idle only */}
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

      {/* Dim mask + selection box — only if selection overlaps this monitor */}
      {isDraggingOrConfirming && localCssRect && (
        <>
          <DimMask rect={localCssRect} />
          {localCssRect.width > 0 && (
            <SelectionBox
              rect={localCssRect}
              phase={phase}
            />
          )}
        </>
      )}

      {/* Dim full monitor if dragging but selection is entirely on another monitor */}
      {isDraggingOrConfirming && !localCssRect && (
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: "rgba(0,0,0,0.45)",
            pointerEvents: "none",
          }}
        />
      )}

      {/* Live readout — only on the monitor where drag is happening */}
      {phase === "dragging" && localCssRect && physRect && (
        <ReadoutLabel
          cssRect={localCssRect}
          physRect={physRect}
        />
      )}

      {/* Confirm bar — show on every monitor that has part of the selection */}
      {phase === "confirming" && localCssRect && physRect && (
        <ConfirmBar
          cssRect={localCssRect}
          physRect={physRect}
          onConfirm={handleConfirm}
          onRestart={handleRestart}
          onCancel={handleCancel}
        />
      )}

      {phase === "idle" && <HintBanner />}
    </div>
  );
}

// ─── Sub-components ───────────────────────────────────────────────────────────

function DimMask({ rect }: { rect: Rectangle }) {
  const dim = "rgba(0,0,0,0.50)";
  return (
    <>
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

function SelectionBox({ rect, phase }: { rect: Rectangle; phase: Phase }) {
  const handleSize = 8;
  const handles =
    phase === "confirming"
      ? [
          { top: -handleSize / 2, left: -handleSize / 2 },
          { top: -handleSize / 2, right: -handleSize / 2 },
          { bottom: -handleSize / 2, left: -handleSize / 2 },
          { bottom: -handleSize / 2, right: -handleSize / 2 },
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
            background: "#60a5fa",
            borderRadius: 2,
            ...style,
          }}
        />
      ))}
    </div>
  );
}

// Shows local CSS position but physical pixel dimensions
function ReadoutLabel({
  cssRect,
  physRect,
}: {
  cssRect: Rectangle;
  physRect: PhysRect;
}) {
  const OFFSET = 6;
  const above = cssRect.y - OFFSET - 26;
  const top = above > 0 ? above : cssRect.y + cssRect.height + OFFSET;

  return (
    <div
      style={{
        position: "absolute",
        top,
        left: cssRect.x,
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
      {/* Physical dimensions — what backend will use */}
      {physRect.width} × {physRect.height} px
    </div>
  );
}

function ConfirmBar({
  cssRect,
  physRect,
  onConfirm,
  onRestart,
  onCancel,
}: {
  cssRect: Rectangle;
  physRect: PhysRect;
  onConfirm: () => void;
  onRestart: () => void;
  onCancel: () => void;
}) {
  const barHeight = 44;
  const barWidth = 320;
  const MARGIN = 12;

  const viewportHeight = window.innerHeight;
  const belowY = cssRect.y + cssRect.height + MARGIN;
  const aboveY = cssRect.y - barHeight - MARGIN;
  const top =
    belowY + barHeight < viewportHeight ? belowY : Math.max(aboveY, MARGIN);

  const centreX = cssRect.x + cssRect.width / 2 - barWidth / 2;
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
        {physRect.width} × {physRect.height} px
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

// ─── Utils ────────────────────────────────────────────────────────────────────

function base64ToBlob(
  base64: string,
  contentType = "image/jpeg",
  sliceSize = 512 * 1024,
): Blob {
  const byteCharacters = atob(base64);
  const byteArrays: Uint8Array[] = [];
  for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
    const slice = byteCharacters.slice(offset, offset + sliceSize);
    const byteNumbers = new Uint8Array(slice.length);
    for (let i = 0; i < slice.length; i++) byteNumbers[i] = slice.charCodeAt(i);
    byteArrays.push(byteNumbers);
  }
  return new Blob(byteArrays as Uint8Array<ArrayBuffer>[], {
    type: contentType,
  });
}
