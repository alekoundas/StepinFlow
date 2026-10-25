import { useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { Message } from "primereact/message";
import { Password } from "primereact/password";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { RepositoryPullDto, RepositoryStatusDto } from "@/shared/models/repository-dtos";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import BranchSwitchDialogComponent from "@/features/repository/components/BranchSwitchDialogComponent";
import { useConnectRemote, usePreviewPull, usePull, usePush } from "@/features/repository/hooks/use-repository";
import { MONO, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  status: RepositoryStatusDto;
}

/**
 * The remote: where it is, the token that signs in to it, and Connect, Push and Pull. Push and
 * Pull act on the branch checked out, against the remote's branch of the same name.
 */
export default function RepositoryRemoteComponent({ status }: Props) {
  const { data: settings = [] } = useAppSettings();
  const { openConfirm } = useDialogStore();

  const [url, setUrl] = useState(status.remoteUrl ?? "");
  const [token, setToken] = useState("");
  const [note, setNote] = useState<string | null>(null);
  const [pullingBranch, setPullingBranch] = useState<string | null>(null);

  const connect = useConnectRemote();
  const preview = usePreviewPull();
  const pull = usePull();
  const push = usePush();

  const isConnected = status.remoteUrl !== null;
  const isTokenSet = settings.find((x) => x.key === AppSettingKeyEnum.GIT_REMOTE_TOKEN)?.isSet ?? false;
  const isBusy = connect.isPending || preview.isPending || push.isPending;
  const error = connect.error ?? preview.error ?? push.error;

  const startPull = (dto: RepositoryPullDto) => {
    setPullingBranch(dto.branch);
    pull.mutate(dto.remoteSha, {
      onSuccess: (result) => {
        setPullingBranch(null);
        setNote(result.backupBranch ? `Pulled. What was never pushed is kept on ${result.backupBranch}.` : "Pulled.");
      },
    });
  };

  const askToPull = (dto: RepositoryPullDto) =>
    openConfirm("repository-pull", {
      headerText: `Pull ${dto.branch} from the remote?`,
      confirmLabel: "Pull",
      confirmIcon: "pi pi-arrow-down",
      confirmSeverity: dto.dropped.length > 0 ? "warning" : undefined,
      width: "min(620px, 94vw)",
      children: <PullSummaryComponent pull={dto} />,
      onConfirm: () => startPull(dto),
    });

  const onConnect = () => {
    setNote(null);
    connect.mutate(
      { url, token },
      {
        onSuccess: (result) => {
          setToken("");

          if (result.pushedBranches.length > 0) setNote(`Connected. Sent ${result.pushedBranches.join(", ")} to the remote.`);
          else if (result.pull && result.pull.incoming.length > 0) askToPull(result.pull);
          else setNote("Connected.");
        },
      },
    );
  };

  const onPull = () => {
    setNote(null);
    preview.mutate(undefined, {
      onSuccess: (result) => {
        if (result.incoming.length === 0 && result.dropped.length === 0) setNote("Already up to date.");
        else askToPull(result);
      },
    });
  };

  const onPush = () => {
    setNote(null);
    push.mutate(undefined, {
      onSuccess: (result) =>
        setNote(
          result.commitCount === 0
            ? "Nothing to push: the remote has every commit."
            : `Sent ${plural(result.commitCount, "commit")} to ${result.branch} on the remote.`,
        ),
    });
  };

  const closePull = () => {
    setPullingBranch(null);
    pull.reset();
  };

  return (
    <div className="flex flex-column gap-3">
      <div className="flex align-items-center justify-content-between flex-wrap gap-2">
        <span className="font-semibold">Remote</span>
        <Tag
          value={isConnected ? "Connected" : "Not connected"}
          severity={isConnected ? "success" : "secondary"}
        />
      </div>

      <LabelComponent
        text="Optional. A remote keeps a copy of the history on a server such as GitHub, GitLab or Azure DevOps, so another computer can work on the same flows."
        size="sm"
        color="secondary"
      />

      <div className="flex flex-column gap-2">
        <label
          htmlFor="repository-remote-url"
          className="font-medium"
        >
          Address
        </label>
        <InputText
          id="repository-remote-url"
          type="url"
          value={url}
          placeholder="https://github.com/you/flows.git"
          onChange={(e) => setUrl(e.target.value)}
          style={{ fontFamily: MONO }}
        />
        <span
          className="text-xs"
          style={MUTED}
        >
          HTTPS only. An SSH address, one starting with git@, is refused.
        </span>
      </div>

      <div className="flex flex-column gap-2">
        <label
          htmlFor="repository-remote-token"
          className="font-medium"
        >
          Token
        </label>
        <Password
          inputId="repository-remote-token"
          value={token}
          placeholder={isTokenSet ? "Already set" : "Paste a token"}
          onChange={(e) => setToken(e.target.value)}
          feedback={false}
          toggleMask
          inputClassName="w-full"
        />
        <span
          className="text-xs"
          style={MUTED}
        >
          Made on the provider’s site, for this repository only, allowed to read and write its contents. Kept on this computer.
        </span>
      </div>

      <Message
        severity="info"
        text="Connecting to an empty remote sends it this history. A remote that already has commits replaces this one: what is here now is kept on a backup branch, and the flows follow the remote."
      />

      <div
        className="flex align-items-center flex-wrap gap-2 pt-2 border-top-1"
        style={{ borderColor: "var(--surface-border)" }}
      >
        <Button
          type="button"
          label="Connect"
          onClick={onConnect}
          loading={connect.isPending}
          disabled={isBusy || url.trim().length === 0}
        />
        <Button
          type="button"
          label="Push"
          icon="pi pi-arrow-up"
          onClick={onPush}
          loading={push.isPending}
          disabled={isBusy || !isConnected}
          outlined
        />
        <Button
          type="button"
          label="Pull"
          icon="pi pi-arrow-down"
          onClick={onPull}
          loading={preview.isPending}
          disabled={isBusy || !isConnected || status.changeCount > 0}
          outlined
        />
        <span
          className="text-xs flex-1"
          style={{ ...MUTED, minWidth: 240 }}
        >
          A pull takes the remote’s version and replaces what is here. What was here is kept on a backup branch first.
        </span>
      </div>

      {isConnected && (
        <LabelComponent
          text={whereOf(status)}
          size="sm"
          color="secondary"
        />
      )}

      {error && (
        <LabelComponent
          text={error.message}
          color="error"
        />
      )}

      {note && (
        <LabelComponent
          text={note}
          color="success"
        />
      )}

      {pullingBranch !== null && (
        <BranchSwitchDialogComponent
          action="Pulling"
          branch={pullingBranch}
          error={pull.error?.message ?? null}
          onClose={closePull}
        />
      )}
    </div>
  );
}

// "Push and Pull act on main: 2 commits to push, 1 to pull, as of the last fetch."
const whereOf = (status: RepositoryStatusDto): string => {
  const subject = `Push and Pull act on ${status.branch}, the branch checked out`;

  if (status.changeCount > 0) return `${subject}. Commit or discard the ${plural(status.changeCount, "change")} to pull.`;
  if (!status.isBranchOnRemote) return `${subject}. The remote does not have it yet: Push sends it.`;
  if (status.aheadCount === 0 && status.behindCount === 0) return `${subject}. It matches the remote, as of the last fetch.`;

  return `${subject}: ${plural(status.aheadCount, "commit")} to push, ${status.behindCount} to pull, as of the last fetch.`;
};

function PullSummaryComponent({ pull }: { pull: RepositoryPullDto }) {
  return (
    <div className="flex flex-column gap-3">
      <span>
        {pull.incoming.length > 0
          ? `${plural(pull.incoming.length, "commit")} arrive from the remote, and every flow is brought to its version.`
          : "Nothing arrives from the remote; the branch only goes back to what the remote has."}
      </span>

      {pull.dropped.length > 0 && (
        <>
          <span style={{ color: "var(--yellow-500)" }}>
            {pull.dropped.length === 1 ? "This commit was" : `These ${pull.dropped.length} commits were`} never pushed. They leave{" "}
            {pull.branch}, and are kept on a backup branch named backup/ and the date and time.
          </span>
          <ul className="list-none m-0 p-0 flex flex-column gap-1">
            {pull.dropped.map((commit) => (
              <li
                key={commit.sha}
                className="flex gap-3 text-sm"
              >
                <span style={{ ...MUTED, fontFamily: MONO }}>{commit.shortSha}</span>
                <span>{commit.message.split("\n")[0]}</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
