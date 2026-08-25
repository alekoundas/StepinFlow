import { useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import {
  BroadcastTypeEnum,
  RecordedInputTypeEnum,
  type RecordedInput,
} from "../../../../../electron/shared/types";
import { AppSettingKindEnum } from "@/shared/enums/backend/app-setting-key-enum";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import {
  useAppSettingMutations,
  useAppSettings,
} from "@/features/settings/hooks/use-app-settings";

/**
 * The debugger's global key bindings.
 *
 * Recorded through the same global hook that will later match them, because while a flow runs the
 * focused window belongs to the application being automated - a key handler in this window would
 * never fire at the only moment it matters. Capturing the same way it is matched also means no
 * translation between the browser's key names and the hook's, so a key the hook cannot see can
 * never be bound by accident.
 *
 * The backend only reports keys. What counts as a combination, and when one is finished, is decided
 * here: keys accumulate while they are held, and the first release commits what was down.
 */
export default function HotkeysPanelComponent() {
  const { data: all = [] } = useAppSettings();
  const { setSettingMutation } = useAppSettingMutations();

  const hotkeys = all.filter((x) => x.kind === AppSettingKindEnum.HOTKEY);

  const [capturingKey, setCapturingKey] = useState<string | null>(null);
  const [heldKeys, setHeldKeys] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Written in an effect rather than during render: the subscription below needs the current
  // bindings to spot a clash, but re-subscribing whenever the list identity changes would tear
  // down the capture halfway through a combination being held.
  const hotkeysRef = useRef<AppSettingDto[]>([]);
  useEffect(() => {
    hotkeysRef.current = hotkeys;
  });

  // The authoritative list while keys are down. State mirrors it for display only, because a
  // handler cannot read state it closed over before the first key arrived.
  const heldRef = useRef<string[]>([]);

  useEffect(() => {
    if (!capturingKey) return;

    heldRef.current = [];

    const commit = (combination: string) => {
      setCapturingKey(null);
      setHeldKeys([]);
      heldRef.current = [];
      void backendApiService.System.inputRecordHotkeyStop();

      // Two commands on one combination means one of them can never fire, so it is refused here
      // rather than silently lost.
      const clash = hotkeysRef.current.find(
        (x) => x.key !== capturingKey && x.value === combination,
      );

      if (clash) {
        setError(`${toDisplay(combination)} is already ${clash.label.toLowerCase()}.`);
        return;
      }

      setError(null);
      setSettingMutation.mutate({ key: capturingKey as never, value: combination });
    };

    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.HOTKEY_CAPTURE_EVENT) return;

      const input = message.payload as RecordedInput;

      // The hook's own name for the key, which is what gets stored: nothing translates it.
      const name = input.keyChar;
      if (!name) return;

      if (input.type === RecordedInputTypeEnum.KEY_DOWN) {
        // A held key repeats, so the same one arriving twice is not a second key.
        if (heldRef.current.includes(name)) return;

        heldRef.current = [...heldRef.current, name];
        setHeldKeys(heldRef.current);
        return;
      }

      // Letting one go ends the combination, so what was down is what gets bound.
      if (input.type === RecordedInputTypeEnum.KEY_UP && heldRef.current.length > 0)
        commit(heldRef.current.join("+"));
    });

    // Also covers leaving the page mid-capture, which would otherwise leave the hook
    // broadcasting every key.
    return () => {
      unsubscribe?.();
      void backendApiService.System.inputRecordHotkeyStop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [capturingKey]);

  const rebind = async (setting: AppSettingDto) => {
    setError(null);
    setHeldKeys([]);
    setCapturingKey(setting.key);
    await backendApiService.System.inputRecordHotkeyStart();
  };

  const cancel = async () => {
    setCapturingKey(null);
    setHeldKeys([]);
    await backendApiService.System.inputRecordHotkeyStop();
  };

  const clear = (setting: AppSettingDto) =>
    setSettingMutation.mutate({ key: setting.key, value: "" });

  return (
    <Panel header="Debugger keys">
      <LabelComponent
        text="Pressed while you are looking at the application being automated, not at StepinFlow, so these are matched system wide. That also means they still do whatever they normally do in that application - rebind any that clash with what a flow drives."
        size="sm"
        color="secondary"
      />

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start mt-3"
          text={error}
        />
      )}

      <div className="flex flex-column gap-3 mt-4">
        {hotkeys.map((setting) => (
          <div
            key={setting.key}
            className="flex align-items-center justify-content-between gap-3"
          >
            <div className="flex flex-column">
              <LabelComponent text={setting.label} />
              <LabelComponent
                text={setting.description}
                size="xs"
                color="secondary"
              />
            </div>

            <div className="flex align-items-center gap-2 flex-shrink-0">
              {capturingKey === setting.key ? (
                <>
                  <span
                    className="font-mono text-sm"
                    style={{ minWidth: "9rem", textAlign: "right" }}
                  >
                    {heldKeys.length > 0 ? (
                      toDisplay(heldKeys.join("+"))
                    ) : (
                      <span className="text-color-secondary">press the keys...</span>
                    )}
                  </span>

                  <Button
                    type="button"
                    label="Cancel"
                    onClick={() => void cancel()}
                    className="p-button-text p-button-sm"
                  />
                </>
              ) : (
                <>
                  <span
                    className="font-mono text-sm"
                    style={{ minWidth: "9rem", textAlign: "right" }}
                  >
                    {setting.value ? (
                      toDisplay(setting.value)
                    ) : (
                      <span className="text-color-secondary">not bound</span>
                    )}
                  </span>

                  <Button
                    type="button"
                    label="Rebind"
                    onClick={() => void rebind(setting)}
                    disabled={capturingKey !== null}
                    className="p-button-outlined p-button-sm"
                  />

                  <Button
                    type="button"
                    icon="pi pi-times"
                    aria-label={`Clear the ${setting.label} binding`}
                    onClick={() => clear(setting)}
                    disabled={!setting.value || capturingKey !== null}
                    className="p-button-text p-button-sm"
                  />
                </>
              )}
            </div>
          </div>
        ))}
      </div>

      <LabelComponent
        text="Hold the keys you want, then let one go to save it."
        size="xs"
        color="secondary"
        className="mt-3"
      />
    </Panel>
  );
}

/**
 * Stored in the hook's vocabulary - "VcLeftControl+VcF10" - because that is what gets matched. The
 * Vc prefix is the hook's, not something a person should have to read.
 */
const toDisplay = (combination: string): string =>
  combination
    .split("+")
    .map((part) => (part.startsWith("Vc") ? part.slice(2) : part))
    .join(" + ");
