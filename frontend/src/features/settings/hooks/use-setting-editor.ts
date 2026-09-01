import { useState } from "react";

import { useAppSettingMutations } from "@/features/settings/hooks/use-app-settings";
import type { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";

/**
 * What has been typed into a settings panel but not yet saved.
 *
 * Committed on blur rather than on every keystroke, because a half typed number or url is not a
 * setting. A dropdown or a switch has nothing to type incrementally, so it writes straight through.
 *
 * Shared because there are two panels: the generic one that renders whatever the catalog says, and
 * the AI one that cannot be generic - what it shows depends on the provider, and its model list has
 * to be fetched from that provider. They differ in what they render, never in when a value is saved.
 */
export function useSettingEditor() {
  const { setSettingMutation } = useAppSettingMutations();

  const [editing, setEditing] = useState<Record<string, string>>({});

  /** What the control shows: what is being typed, or what is saved. */
  const valueOf = (key: AppSettingKeyEnum, saved: string): string =>
    editing[key] ?? saved;

  const edit = (key: AppSettingKeyEnum, value: string) =>
    setEditing((previous) => ({ ...previous, [key]: value }));

  const commit = (key: AppSettingKeyEnum, saved: string) => {
    const value = editing[key];
    if (value == null) return;

    setEditing((previous) => {
      const next = { ...previous };
      delete next[key];
      return next;
    });

    if (value !== saved) setSettingMutation.mutate({ key: key, value: value });
  };

  /** Written straight through: there is nothing to type incrementally in a switch or a dropdown. */
  const commitNow = (key: AppSettingKeyEnum, value: string) =>
    setSettingMutation.mutate({ key: key, value: value });

  return { valueOf, edit, commit, commitNow };
}
