import { useCallback, useEffect, useRef, useState } from "react";

import { backendApiService } from "@/shared/services/backend-api-service";
import type {
  IpcBroadcastMessage,
  RecordedInput,
} from "../../../../../electron/shared/types";

/**
 * Both sides of a key are the same shortcut to a reader, and the backend resolves an unqualified
 * modifier to the left one.
 */
const MODIFIER_NAMES: Record<string, string> = {
  LeftCtrl: "Ctrl",
  RightCtrl: "Ctrl",
  LeftAlt: "Alt",
  RightAlt: "Alt",
  LeftShift: "Shift",
  RightShift: "Shift",
  LeftMeta: "Win",
  RightMeta: "Win",
};

// The order KeyCombinationHelper reads them back in, so a captured combination and a recorded one
// are the same string.
const MODIFIER_ORDER = ["Ctrl", "Alt", "Shift", "Win"];

/**
 * The text form of what was held, or null when nothing but modifiers was.
 *
 * Key names are KeyCodeEnum members untranslated, because that is what the backend parses back
 * into keys. Anything invented here would be a combination the engine cannot press.
 */
export const toCombination = (keyCodes: string[]): string | null => {
  const pressed = keyCodes.filter((x) => !(x in MODIFIER_NAMES));
  if (pressed.length === 0) return null;

  const modifiers = MODIFIER_ORDER.filter((name) =>
    keyCodes.some((x) => MODIFIER_NAMES[x] === name),
  );

  // The last one, so rolling off a combination binds the key that finished it.
  return [...modifiers, pressed[pressed.length - 1]].join("+");
};

/** What to show while keys are still down. Modifiers only is a legitimate half-way state. */
export const toDisplay = (keyCodes: string[]): string =>
  keyCodes.map((x) => MODIFIER_NAMES[x] ?? x).join("+");

interface Props {
  captureCombination: () => Promise<string | null>;
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

  const captureCombination = useCallback((): Promise<string | null> => {
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
        backendApiService.System.inputRecordHotkeyStop().catch(() => {});
        setIsCapturing(false);
        setHeldKeys([]);
        heldRef.current = [];
        resolve(combination);
      };

      teardownRef.current = () => finish(null);

      unsubscribe = backendApiService.OnBroadcast(
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

          const combination = toCombination(heldRef.current);

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

      backendApiService.System.inputRecordHotkeyStart().catch(() => finish(null));
    });
  }, []);

  const cancelCapture = useCallback(() => {
    teardownRef.current?.();
  }, []);

  // Never leave the backend broadcasting every key because the form was closed mid capture.
  useEffect(() => () => teardownRef.current?.(), []);

  return { captureCombination, cancelCapture, isCapturing, heldKeys };
}
