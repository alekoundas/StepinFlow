import type { CSSProperties } from "react";

import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { toHotkeyDisplay } from "@/shared/utils/hotkey";

const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

/** What happens once Record now is pressed. The keys are the ones in Settings, so what this says is what works. */
export default function WhileYouRecordComponent() {
  const { data: settings = [] } = useAppSettings();

  const pauseKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_PAUSE)?.value;
  const stopKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_STOP)?.value;
  const checkKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_CHECK)?.value;
  const substituteKeys = settings.find((x) => x.key === AppSettingKeyEnum.RECORDING_HOTKEY_SUBSTITUTE)?.value;

  return (
    <section
      className="flex flex-column gap-3 p-4 border-1 border-round-xl"
      style={CARD}
    >
      <h2 className="m-0 text-base font-semibold">While you record</h2>
      <p
        className="m-0"
        style={MUTED}
      >
        StepinFlow steps aside to a small bar that stays on top. Arrange your windows, then press
        Start on it. Clicks on the bar are never recorded, and it never shows up in a screenshot.
      </p>
      <dl
        className="m-0 align-items-center"
        style={{ display: "grid", gridTemplateColumns: "auto 1fr", gap: "10px 14px" }}
      >
        <dt>
          <KeysComponent keys={pauseKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          Pause or resume
        </dd>
        <dt>
          <KeysComponent keys={stopKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          Stop and review
        </dd>
        <dt>
          <KeysComponent keys={checkKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          Check what you click on: that it is there, or that it goes
        </dd>
        <dt>
          <KeysComponent keys={substituteKeys} />
        </dt>
        <dd
          className="m-0"
          style={MUTED}
        >
          A step that is not a click: a command, an application, an input
        </dd>
      </dl>
      <span
        className="text-xs"
        style={MUTED}
      >
        They work whatever window is in front. The keys are never recorded, the two clicks never reach
        the application, and Review asks what each click was for. All four are under Settings,
        Recording keys.
      </span>
    </section>
  );
}

function KeysComponent({ keys }: { keys: string | undefined }) {
  if (!keys)
    return (
      <span
        className="text-sm"
        style={MUTED}
      >
        Not set
      </span>
    );

  return (
    <kbd
      className="text-xs border-1 border-round-md px-2 py-1"
      style={{ fontFamily: "inherit", borderColor: "var(--surface-border)", borderBottomWidth: 2 }}
    >
      {toHotkeyDisplay(keys)}
    </kbd>
  );
}
