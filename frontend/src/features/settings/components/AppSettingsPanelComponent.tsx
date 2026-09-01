import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { Password } from "primereact/password";
import { Dropdown } from "primereact/dropdown";
import { InputSwitch } from "primereact/inputswitch";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { AppSettingDto } from "@/shared/models/database/app-setting-dto";
import { AppSettingKindEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { useSettingEditor } from "@/features/settings/hooks/use-setting-editor";

interface Props {
  header: string;
  description: string;

  /** Which settings belong to this panel. The catalog decides what exists, this decides where. */
  keyPrefix: string;

  /** Shown after a number, when the number has a unit worth saying. */
  numberSuffix?: string;
}

/**
 * A group of settings, rendered from what the backend says rather than from a hardcoded list, so
 * adding a setting is a definition on one side only. What control each one gets comes from its
 * kind - the page never branches on the key.
 */
export default function AppSettingsPanelComponent({
  header,
  description,
  keyPrefix,
  numberSuffix,
}: Props) {
  const { data: all = [], isLoading } = useAppSettings();
  const { valueOf, edit, commit, commitNow } = useSettingEditor();

  const settings = all.filter((x) => x.key.startsWith(keyPrefix));

  const control = (setting: AppSettingDto) => {
    switch (setting.kind) {
      case AppSettingKindEnum.INT:
        return (
          <InputNumber
            value={Number(valueOf(setting.key, setting.value))}
            min={setting.minimum ?? undefined}
            max={setting.maximum ?? undefined}
            onValueChange={(e) => edit(setting.key, String(e.value ?? ""))}
            onBlur={() => commit(setting.key, setting.value)}
            showButtons
            suffix={numberSuffix}
            inputClassName="w-6rem"
            className="flex-shrink-0"
          />
        );

      case AppSettingKindEnum.CHOICE:
        return (
          <Dropdown
            value={valueOf(setting.key, setting.value)}
            options={setting.options}
            onChange={(e) => commitNow(setting.key, e.value)}
            className="w-12rem"
          />
        );

      case AppSettingKindEnum.BOOL:
        return (
          <InputSwitch
            checked={valueOf(setting.key, setting.value) === "true"}
            onChange={(e) => commitNow(setting.key, e.value ? "true" : "false")}
          />
        );

      case AppSettingKindEnum.SECRET:
        return (
          <Password
            value={valueOf(setting.key, setting.value)}
            placeholder={setting.isSet ? "Already set" : "Not set"}
            onChange={(e) => edit(setting.key, e.target.value)}
            onBlur={() => commit(setting.key, setting.value)}
            feedback={false}
            toggleMask
            className="w-14rem"
          />
        );

      default:
        return (
          <InputText
            value={valueOf(setting.key, setting.value)}
            onChange={(e) => edit(setting.key, e.target.value)}
            onBlur={() => commit(setting.key, setting.value)}
            className="w-14rem"
          />
        );
    }
  };

  return (
    <Panel header={header}>
      <LabelComponent
        text={description}
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
            <div className="flex flex-column flex-1 min-w-0">
              <LabelComponent text={setting.label} />
              <LabelComponent
                text={setting.description}
                size="xs"
                color="secondary"
              />
            </div>

            {control(setting)}
          </div>
        ))}
      </div>
    </Panel>
  );
}
