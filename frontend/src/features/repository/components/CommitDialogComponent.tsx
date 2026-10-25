import { useState } from "react";
import type { CSSProperties, KeyboardEvent } from "react";
import { Button } from "primereact/button";
import { Checkbox } from "primereact/checkbox";
import { Dialog } from "primereact/dialog";
import { InputTextarea } from "primereact/inputtextarea";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import type { RepositoryFileChangeDto } from "@/shared/models/repository-dtos";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import ChangeKindBadgeComponent from "@/features/repository/components/ChangeKindBadgeComponent";
import {
  useCommitRepository,
  useRepositoryChanges,
  useRepositoryStatus,
} from "@/features/repository/hooks/use-repository";
import { MONO, describeChanges, imageSource, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  visible: boolean;

  /** The flow being edited: its files start ticked, everyone else's do not. */
  flowId: number | undefined;
  onHide: () => void;
}

/** Every file saved since the last commit, the ones to commit ticked, and a message saying why. */
export default function CommitDialogComponent({ visible, flowId, onHide }: Props) {
  const { data: files, isLoading, error } = useRepositoryChanges(visible);

  return (
    <Dialog
      header="Commit changes"
      visible={visible}
      onHide={onHide}
      style={{ width: "min(720px, 94vw)" }}
      modal
    >
      {isLoading && (
        <LabelComponent
          text="Reading what changed..."
          color="secondary"
        />
      )}

      {error && (
        <Message
          severity="error"
          text={error instanceof Error ? error.message : String(error)}
        />
      )}

      {/* Mounted per opening, so what is ticked and the message start over from what changed. */}
      {visible && files && (
        <CommitFormComponent
          files={files}
          flowId={flowId}
          onDone={onHide}
        />
      )}
    </Dialog>
  );
}

interface FormProps {
  files: RepositoryFileChangeDto[];
  flowId: number | undefined;
  onDone: () => void;
}

function CommitFormComponent({ files, flowId, onDone }: FormProps) {
  const { data: status } = useRepositoryStatus();
  const { data: settings = [] } = useAppSettings();
  const commitMutation = useCommitRepository();

  const isOwn = (file: RepositoryFileChangeDto) => flowId === undefined || file.flowId === flowId;
  const own = files.filter(isOwn);

  const [picked, setPicked] = useState<Set<string>>(() => new Set(own.map((x) => x.path)));
  const [message, setMessage] = useState(() => describeChanges(own[0]?.flowName ?? "", own));
  const [error, setError] = useState<string | null>(null);

  if (files.length === 0)
    return (
      <div className="flex flex-column gap-3">
        <p
          className="m-0"
          style={MUTED}
        >
          Nothing to commit: every flow is as it was at the last commit.
        </p>
        <div className="flex justify-content-end">
          <Button
            type="button"
            label="Close"
            onClick={onDone}
          />
        </div>
      </div>
    );

  // The flow being edited first, then the rest by name, each with its script before its images.
  const groups = groupByFlow(files, isOwn);

  const toggle = (path: string) =>
    setPicked((previous) => {
      const next = new Set(previous);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });

  const canCommit = picked.size > 0 && message.trim().length > 0;

  const commit = async () => {
    if (!canCommit) return;
    setError(null);

    try {
      await commitMutation.mutateAsync({ message: message.trim(), paths: [...picked] });
      onDone();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && e.ctrlKey) {
      e.preventDefault();
      void commit();
    }
  };

  const settingOf = (key: AppSettingKeyEnum) => settings.find((x) => x.key === key)?.value.trim() ?? "";
  const authorName = settingOf(AppSettingKeyEnum.GIT_AUTHOR_NAME) || status?.configuredAuthorName || "";
  const authorEmail = settingOf(AppSettingKeyEnum.GIT_AUTHOR_EMAIL) || status?.configuredAuthorEmail || "";

  return (
    <div className="flex flex-column gap-3">
      <div className="flex flex-column gap-2">
        <label
          htmlFor="commit-message"
          className="font-medium"
        >
          Message
        </label>
        <InputTextarea
          id="commit-message"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          onKeyDown={onKeyDown}
          rows={2}
          autoResize
          autoFocus
        />
        <span
          className="text-xs"
          style={MUTED}
        >
          Filled in from what changed. Ctrl+Enter commits.
        </span>
      </div>

      <div className="flex flex-column gap-2">
        <div className="flex align-items-center justify-content-between gap-3">
          <span className="font-medium">Files</span>
          <span
            className="text-xs"
            style={MUTED}
          >
            {picked.size} of {plural(files.length, "file")}
          </span>
        </div>

        <div
          className="flex flex-column p-1 border-round-lg"
          style={{ border: "1px solid var(--surface-border)", background: "var(--surface-ground)", maxHeight: 360, overflowY: "auto" }}
        >
          {groups.map((group) => (
            <div key={group.name}>
              <div
                className="px-2 pt-2 pb-1 text-xs"
                style={MUTED}
              >
                {group.name || "Repository"}
                {group.isOwn && flowId !== undefined ? " · this flow" : ""}
              </div>

              {group.files.map((file) => {
                const inputId = `commit-file-${files.indexOf(file)}`;
                const slash = file.path.lastIndexOf("/");

                return (
                  <div
                    key={file.path}
                    className="flex align-items-center gap-2 px-2 border-round"
                    style={{ minHeight: 40 }}
                  >
                    <Checkbox
                      inputId={inputId}
                      checked={picked.has(file.path)}
                      onChange={() => toggle(file.path)}
                    />
                    <ChangeKindBadgeComponent kind={file.kind} />
                    {file.isImage && file.image && (
                      <img
                        src={imageSource(file.path, file.image)}
                        alt=""
                        className="flex-none border-round-sm"
                        style={{ width: 36, height: 24, objectFit: "contain", background: "#ffffff" }}
                      />
                    )}
                    <label
                      htmlFor={inputId}
                      className="flex-1 min-w-0 white-space-nowrap overflow-hidden text-overflow-ellipsis text-sm cursor-pointer"
                      style={{ fontFamily: MONO }}
                    >
                      <span style={MUTED}>{file.path.slice(0, slash + 1)}</span>
                      {file.path.slice(slash + 1)}
                    </label>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>

      {error && (
        <Message
          severity="error"
          text={error}
        />
      )}

      <div className="flex align-items-center justify-content-between flex-wrap gap-3">
        <span
          className="text-xs"
          style={MUTED}
        >
          To <span style={{ fontFamily: MONO }}>{status?.branch ?? "main"}</span>
          {authorName && `, as ${authorName}`}
          {authorEmail && ` · ${authorEmail}`}
        </span>

        <div className="flex gap-2">
          <Button
            type="button"
            label="Cancel"
            onClick={onDone}
            severity="secondary"
            text
          />
          <Button
            type="button"
            label={`Commit ${plural(picked.size, "file")}`}
            icon="pi pi-check-circle"
            onClick={() => void commit()}
            loading={commitMutation.isPending}
            disabled={!canCommit}
          />
        </div>
      </div>
    </div>
  );
}

interface FileGroup {
  name: string;
  isOwn: boolean;
  files: RepositoryFileChangeDto[];
}

const groupByFlow = (
  files: RepositoryFileChangeDto[],
  isOwn: (file: RepositoryFileChangeDto) => boolean,
): FileGroup[] => {
  const groups: FileGroup[] = [];

  for (const file of files) {
    let group = groups.find((x) => x.name === file.flowName);
    if (!group) {
      group = { name: file.flowName, isOwn: isOwn(file), files: [] };
      groups.push(group);
    }

    group.files.push(file);
  }

  return groups.sort((a, b) => Number(b.isOwn) - Number(a.isOwn));
};
