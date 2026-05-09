/**
 * Cross-monitor selection overlay.
 *
 * Coordinate contract:
 *  - All selection state (startPhys, endPhys) is in PHYSICAL ABSOLUTE coords
 *  - logical coords are used ONLY at the edges (mouse input → broadcast,
 *    broadcast → render clip)
 *
 * Flow:
 *  1. signalReady()      -> Get screenshot + monitor metadata
 *  2. Broadcast listener -> Listen for broadcasted mouse events and convert to physical rect to logical rect
 *  4. render             -> Use logical rect for rendering selection + dim
 *  5. confirm            -> send physical absolute rect via signalCloseWindow()
 */

import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { Point, Rectangle } from "electron";
import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import type {
  IpcBroadcastMessage,
  RecordedInput,
  SignalReadyResponse,
} from "../../../../electron/shared/types";
import OverlaySelectionDimmer from "@/windows/overlay/components/OverlaySelectionDimmer";
import OverlaySelection from "@/windows/overlay/components/OverlaySelection";
import OverlayConfirmBar from "@/windows/overlay/components/OverlayConfirmBar";

export type Phase = "idle" | "dragging" | "confirming";

// ============================================================================
// Helpers
// ============================================================================
function createRectangle(a: Point, b: Point): Rectangle {
  const x = Math.min(a.x, b.x);
  const y = Math.min(a.y, b.y);
  return {
    x,
    y,
    width: Math.abs(b.x - a.x),
    height: Math.abs(b.y - a.y),
  };
}

export default function OverlayCapturePage() {
  const [screenshot, setScreenshot] = useState<string | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const startPhysRef = useRef<Point | null>(null);
  const phaseRef = useRef<Phase>("idle");

  // Selection stored in physical absolute coords
  const [startPhys, setStartPhys] = useState<Point | null>(null);
  const [endPhys, setEndPhys] = useState<Point | null>(null);

  // Monitor metadata — stable refs (set once on ready, never change)
  const scaleFactor = useRef(1);
  const monitorLogicalOrigin = useRef<Point>({ x: 0, y: 0 });
  const logicalSize = useRef({ width: 0, height: 0 });

  // Clip a physical absolute rect to this monitor and return local CSS rect.
  // Returns null if the selection doesn't overlap this monitor at all.
  const physicalToLogicalRect = useCallback(
    (physRect: Rectangle): Rectangle | null => {
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
    },
    [],
  );

  // ============================================================================
  // Signal ready
  // ============================================================================
  useLayoutEffect(() => {
    ElectronApiService.overlay
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
      .catch((err: any) => console.error("[Overlay] signalReady failed:", err));
  }, []);

  // ============================================================================
  // Broadcast listener  -  single source of truth for all state
  // ============================================================================
  useEffect(() => {
    const unsub = ElectronApiService.backendApi.OnBroadcast(
      (event: IpcBroadcastMessage<RecordedInput>) => {
        const pt: Point = {
          x: event.payload.physicalX,
          y: event.payload.physicalY,
        };

        if (event.payload.type === "BUTTON_DOWN") {
          if (phaseRef.current === "confirming") return;
          setStartPhys(pt);
          setEndPhys(pt);
          setPhase("dragging");
        } else if (event.payload.type === "CURSOR_DRAG") {
          if (phaseRef.current === "confirming") return;
          setEndPhys(pt);
        } else if (event.payload.type === "BUTTON_UP") {
          if (phaseRef.current === "confirming") return;
          setEndPhys(pt);
          // Too small = accidental click → back to idle
          if (startPhysRef.current) // ← ref, not state
          {
            const r = createRectangle(startPhysRef.current, pt); // ← ref, not state
            if (r.width < 4 || r.height < 4) {
              setPhase("idle");
              setStartPhys(null);
              setEndPhys(null);
              return;
            }
          }
          setPhase("confirming");
        } else if (event.payload.type === "KEY_UP") {
          // ESC cancels selection
          if (event.payload.keyCode === "Escape") {
            sendResult(null);
          }
        }
      },
    );
    return unsub;
  }, []);

  useEffect(() => {
    startPhysRef.current = startPhys;
  }, [startPhys]);

  useEffect(() => {
    phaseRef.current = phase;
  }, [phase]);

  // ── Result ─────────────────────────────────────────────────────────────────

  const sendResult = useCallback((rect: Rectangle | null) => {
    ElectronApiService.overlay.signalCloseWindow(rect);
  }, []);

  const handleConfirm = useCallback(() => {
    if (!startPhys || !endPhys) return;
    // Send physical absolute rect — backend speaks physical
    sendResult(createRectangle(startPhys, endPhys));
  }, [startPhys, endPhys, sendResult]);

  const handleCancel = useCallback(() => sendResult(null), [sendResult]);
  const 
  onRedraw = useCallback(() => {
    setPhase("idle");
    setStartPhys(null);
    setEndPhys(null);
  }, []);

  const physRect =
    startPhys && endPhys ? createRectangle(startPhys, endPhys) : null;
  const logicalRect = physRect ? physicalToLogicalRect(physRect) : null;
  const isDraggingOrConfirming = phase === "dragging" || phase === "confirming";

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
      for (let i = 0; i < slice.length; i++)
        byteNumbers[i] = slice.charCodeAt(i);
      byteArrays.push(byteNumbers);
    }
    return new Blob(byteArrays as Uint8Array<ArrayBuffer>[], {
      type: contentType,
    });
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        userSelect: "none",
        cursor: phase === "confirming" ? "default" : "crosshair",
        overflow: "hidden",
        pointerEvents: phase === "confirming" ? "auto" : "none", // TODO test
      }}
    >
      {/*========   Display Frozen desktop screenshot   ========*/}
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

      {/*========   Display Dimmer    ========*/}
      <OverlaySelectionDimmer selectionRect={logicalRect} />

      {/*========   Display selection box   ========*/}
      {isDraggingOrConfirming &&
        physRect &&
        logicalRect &&
        logicalRect.width > 0 && (
          <OverlaySelection
            logicalSelectionRect={logicalRect}
            physicalSelectionRect={physRect}
            phase={phase}
          />
        )}

      {/*========   Display Confirm bar (on every monitor that has part of the selection)  ========*/}
      {phase === "confirming" && logicalRect && physRect && (
        <OverlayConfirmBar
          logicalRect={logicalRect}
          physicalRect={physRect}
          onConfirm={handleConfirm}
          onRedraw={onRedraw}
          onCancel={handleCancel}
        />
      )}

      {/*========   Display Hint Banner (top of every monitor)   ========*/}
      {phase === "idle" && (
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
      )}
    </div>
  );
}
