import { useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "primereact/button";

import BranchMenuComponent from "@/features/repository/components/BranchMenuComponent";
import CommitDialogComponent from "@/features/repository/components/CommitDialogComponent";
import { useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import { UNCOMMITTED_COLOR, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  flowId: number | undefined;

  /** The count opens what changed. */
  onShowChanges: () => void;
}

/**
 * The editor header's word on the repository: the branch and its menu, this flow's changes since
 * the last commit, and Commit. Saving writes the flow's files; committing is always this button.
 */
export default function RepositoryChipComponent({ flowId, onShowChanges }: Props) {
  const { data: status, isError } = useRepositoryStatus();
  const [isCommitting, setIsCommitting] = useState(false);

  if (!status) {
    if (!isError) return null;

    return (
      <span
        className="text-xs align-self-center"
        style={MUTED}
        title="The repository is made when the app starts. It may still be writing the flows, or the folder could not be written."
      >
        No repository yet
      </span>
    );
  }

  const count = status.flows.find((x) => x.flowId === flowId)?.changeCount ?? 0;

  return (
    <>
      <div
        className="flex align-items-center border-round-md"
        style={{ border: "1px solid var(--surface-border)" }}
      >
        <BranchMenuComponent
          flowId={flowId}
          branch={status.branch}
          commitCount={status.commitCount}
          changeCount={status.changeCount}
        />

        <span
          aria-hidden="true"
          style={{ width: 1, height: 20, background: "var(--surface-border)" }}
        />

        {count > 0 ? (
          <Button
            type="button"
            onClick={onShowChanges}
            text
            size="small"
            className="gap-2 px-3"
            style={{ color: UNCOMMITTED_COLOR }}
            tooltip="What changed since the last commit"
            tooltipOptions={{ position: "bottom" }}
          >
            <span
              className="border-circle"
              style={{ width: 7, height: 7, background: UNCOMMITTED_COLOR }}
            />
            {plural(count, "change")}
          </Button>
        ) : (
          <span
            className="flex align-items-center gap-2 px-3 py-2 text-sm"
            style={MUTED}
          >
            <i
              className="pi pi-check"
              style={{ color: "var(--green-400)" }}
              aria-hidden="true"
            />
            Committed
          </span>
        )}
      </div>

      {count > 0 && (
        <Button
          type="button"
          label="Commit"
          icon="pi pi-check-circle"
          onClick={() => setIsCommitting(true)}
          outlined
        />
      )}

      <CommitDialogComponent
        visible={isCommitting}
        flowId={flowId}
        onHide={() => setIsCommitting(false)}
      />
    </>
  );
}

