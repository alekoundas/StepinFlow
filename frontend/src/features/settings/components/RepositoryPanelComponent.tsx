import type { CSSProperties } from "react";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { Panel } from "primereact/panel";

import LabelComponent from "@/shared/components/LabelComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { useSettingEditor } from "@/features/settings/hooks/use-setting-editor";
import { useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import RepositoryRemoteComponent from "@/features/repository/components/RepositoryRemoteComponent";
import { MONO, UNCOMMITTED_COLOR, formatWhen, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

/**
 * Where the flows are kept as files, who commits them, and the remote they are pushed to and
 * pulled from. The folder is the app's own and made at first launch.
 */
export default function RepositoryPanelComponent() {
  const { data: status, isError } = useRepositoryStatus();
  const { data: settings = [] } = useAppSettings();
  const { valueOf, edit, commit } = useSettingEditor();

  // Opened inside the folder rather than at it: showing a file selects it in its folder. The
  // separator is the backend's, which is the system's.
  const openFolder = () => {
    if (!status) return;

    const separator = status.folderPath.includes("\\") ? "\\" : "/";
    void ElectronApiService.files.showInFolder(`${status.folderPath}${separator}.gitattributes`);
  };

  const authorField = (key: AppSettingKeyEnum, placeholder: string) => {
    const setting = settings.find((x) => x.key === key);
    if (!setting) return null;

    return (
      <div className="flex align-items-center justify-content-between gap-3">
        <div className="flex flex-column flex-1 min-w-0">
          <LabelComponent text={setting.label} />
          <LabelComponent
            text={setting.description}
            size="xs"
            color="secondary"
          />
        </div>
        <InputText
          value={valueOf(setting.key, setting.value)}
          placeholder={placeholder}
          onChange={(e) => edit(setting.key, e.target.value)}
          onBlur={() => commit(setting.key, setting.value)}
          className="w-16rem"
        />
      </div>
    );
  };

  const fromConfig = (value: string | undefined) => (value ? `${value} (git config)` : "Not set");

  return (
    <Panel header="Repository">
      <LabelComponent
        text="Every flow is kept as its script and its template images, in a git repository on this computer. Saving a flow writes its files; Commit records them."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-3 mt-3">
        {isError && !status && (
          <LabelComponent
            text="No repository yet. It is made when the app starts, and may still be writing the flows."
            size="sm"
            color="warning"
          />
        )}

        {status && (
          <>
            <div className="flex flex-column gap-2">
              <LabelComponent text="Folder" />
              <div className="flex gap-2">
                <InputText
                  value={status.folderPath}
                  readOnly
                  className="flex-1 min-w-0"
                  style={{ fontFamily: MONO }}
                />
                <Button
                  type="button"
                  label="Open folder"
                  icon="pi pi-folder-open"
                  onClick={openFolder}
                  severity="secondary"
                  outlined
                />
              </div>
            </div>

            <div className="flex flex-wrap gap-4 text-sm">
              <span>
                <span style={MUTED}>Branch </span>
                <span style={{ fontFamily: MONO }}>{status.branch}</span>
              </span>
              <span>
                <span style={MUTED}>Commits </span>
                {status.commitCount}
              </span>
              {status.lastCommitAt && (
                <span>
                  <span style={MUTED}>Last commit </span>
                  {formatWhen(status.lastCommitAt)}
                </span>
              )}
              <span style={{ color: status.flows.length > 0 ? UNCOMMITTED_COLOR : undefined }}>
                <span style={MUTED}>Not committed </span>
                {status.flows.length > 0 ? plural(status.flows.length, "flow") : "nothing"}
              </span>
            </div>
          </>
        )}

        {authorField(AppSettingKeyEnum.GIT_AUTHOR_NAME, fromConfig(status?.configuredAuthorName))}
        {authorField(AppSettingKeyEnum.GIT_AUTHOR_EMAIL, fromConfig(status?.configuredAuthorEmail))}

        {status && (
          <div
            className="pt-3 border-top-1"
            style={{ borderColor: "var(--surface-border)" }}
          >
            <RepositoryRemoteComponent status={status} />
          </div>
        )}
      </div>
    </Panel>
  );
}
