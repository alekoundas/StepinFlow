import { useState } from "react";
import { InputNumber } from "primereact/inputnumber";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import {
  useAppSettingMutations,
  useAppSettings,
} from "@/features/settings/hooks/use-app-settings";

/**
 * Every numeric setting the catalog exposes, rendered from what the backend says rather than
 * from a hardcoded list, so adding a setting is a definition on one side only.
 */
export default function AppSettingsPanelComponent() {
  const { data: settings = [], isLoading } = useAppSettings();
  const { setSettingMutation } = useAppSettingMutations();

  // Committed on blur, not on every keystroke: a partially typed number is not a setting.
  const [editing, setEditing] = useState<Record<string, number | null>>({});

  const valueOf = (setting: AppSettingDto): number =>
    editing[setting.key] ?? Number(setting.value);

  const commit = (setting: AppSettingDto) => {
    const value = editing[setting.key];
    if (value == null) return;

    setEditing((previous) => {
      const next = { ...previous };
      delete next[setting.key];
      return next;
    });

    if (String(value) !== setting.value)
      setSettingMutation.mutate({ key: setting.key, value: String(value) });
  };

  return (
    <Panel header="Recording">
      <LabelComponent
        text="How much of the screen is captured around the pointer each time you click while recording. Bigger gives the wizard more to crop a template from."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-3 mt-3">
        {isLoading && (
          <LabelComponent
            text="Loading settings..."
            size="sm"
          />
        )}

        {settings.map((setting) => (
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

            <InputNumber
              value={valueOf(setting)}
              min={setting.minimum ?? undefined}
              max={setting.maximum ?? undefined}
              onValueChange={(e) =>
                setEditing((previous) => ({ ...previous, [setting.key]: e.value ?? null }))
              }
              onBlur={() => commit(setting)}
              showButtons
              suffix=" px"
              className="w-8rem"
            />
          </div>
        ))}
      </div>
    </Panel>
  );
}
