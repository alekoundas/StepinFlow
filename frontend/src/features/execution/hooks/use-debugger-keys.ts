import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";

export interface DebuggerKeys {
  continue?: string;
  stepInto?: string;
  stepOver?: string;
  pause?: string;
  stop?: string;
}

/** The debugger's keys as Settings has them, in the hook's vocabulary, so what a button shows is what works. */
export function useDebuggerKeys(): DebuggerKeys {
  const { data: settings = [] } = useAppSettings();

  const valueOf = (key: AppSettingKeyEnum) =>
    settings.find((x) => x.key === key)?.value || undefined;

  return {
    continue: valueOf(AppSettingKeyEnum.HOTKEY_CONTINUE),
    stepInto: valueOf(AppSettingKeyEnum.HOTKEY_STEP_INTO),
    stepOver: valueOf(AppSettingKeyEnum.HOTKEY_STEP_OVER),
    pause: valueOf(AppSettingKeyEnum.HOTKEY_PAUSE),
    stop: valueOf(AppSettingKeyEnum.HOTKEY_STOP),
  };
}
