import { useCallback, useEffect, useRef, useState } from "react";

import { BackendApiService } from "@/shared/services/backend-api-service";
import type {
  IpcBroadcastMessage,
  RecordedInput,
} from "../../../../../electron/shared/types";
import { KeyCombination } from "@/features/flow-step/key-combination";

interface Props {
  captureCombination: (allowModifiersOnly?: boolean) => Promise<string | null>;
  cancelCapture: () => void;
  isCapturing: boolean;
  heldKeys: string[];
}

/**
 * Arms "press the keys you want".
 *
 * Captured through the global hook rather than a key handler in this window, for the same reason
 * the hotkey settings do it: it is the only way to see keys the browser swallows, and it means no
 * translation between the browser's names for keys and the ones the engine presses.
 *
 * Escape is not a cancel here - it is a key a flow may well need to send - so cancelling is the
 * caller's button.
 */
export function useCaptureCombination(): Props {
  const [isCapturing, setIsCapturing] = useState(false);
  const [heldKeys, setHeldKeys] = useState<string[]>([]);

  const teardownRef = useRef<(() => void) | null>(null);

  // The authoritative list while keys are down: a handler cannot read state it closed over before
  // the first key arrived.
  const heldRef = useRef<string[]>([]);

  const captureCombination = useCallback((allowModifiersOnly = false): Promise<string | null> => {
    if (teardownRef.current) return Promise.resolve(null);

    setIsCapturing(true);
    setHeldKeys([]);
    heldRef.current = [];

    return new Promise<string | null>((resolve) => {
      let unsubscribe: (() => void) | null = null;

      const finish = (combination: string | null) => {
        if (!teardownRef.current) return;
        teardownRef.current = null;

        unsubscribe?.();
        BackendApiService.System.inputRecordHotkeyStop().catch(() => {});
        setIsCapturing(false);
        setHeldKeys([]);
        heldRef.current = [];
        resolve(combination);
      };

      teardownRef.current = () => finish(null);

      unsubscribe = BackendApiService.onBroadcast(
        (event: IpcBroadcastMessage<RecordedInput>) => {
          if (event.type !== "HOTKEY_CAPTURE_EVENT") return;

          const keyCode = event.payload.keyCode;
          if (!keyCode) return;

          if (event.payload.type === "KEY_DOWN") {
            // A held key repeats, so the same one arriving twice is not a second key.
            if (heldRef.current.includes(keyCode)) return;

            heldRef.current = [...heldRef.current, keyCode];
            setHeldKeys(heldRef.current);
            return;
          }

          if (event.payload.type !== "KEY_UP" || heldRef.current.length === 0) return;

          const combination = KeyCombination.toCombination(heldRef.current, allowModifiersOnly);

          // Letting go of a modifier having pressed nothing else is not a combination the engine
          // can press, so it starts over rather than committing something that would fail.
          if (combination === null) {
            heldRef.current = [];
            setHeldKeys([]);
            return;
          }

          finish(combination);
        },
      );

      BackendApiService.System.inputRecordHotkeyStart().catch(() => finish(null));
    });
  }, []);

  const cancelCapture = useCallback(() => {
    teardownRef.current?.();
  }, []);

  // Never leave the backend broadcasting every key because the form was closed mid capture.
  useEffect(() => () => teardownRef.current?.(), []);

  return { captureCombination, cancelCapture, isCapturing, heldKeys };
}
