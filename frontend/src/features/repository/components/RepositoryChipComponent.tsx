import { useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "primereact/button";

import CommitDialogComponent from "@/features/repository/components/CommitDialogComponent";
import { useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import { MONO, UNCOMMITTED_COLOR, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  flowId: number | undefined;

  /** The count opens what changed. */
  onShowChanges: () => void;
}

/**
 * The editor header's word on the repository: the branch, this flow's changes since the last
 * commit, and Commit. Saving writes the flow's files; committing is always this button.
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
        <span
          className="flex align-items-center gap-2 px-3 py-2 text-sm"
          title="The repository's branch"
          style={{ fontFamily: MONO }}
        >
          <BranchIconComponent />
          {status.branch}
        </span>

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

// PrimeIcons has no branch.
function BranchIconComponent() {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle
        cx="6"
        cy="5"
        r="2"
      />
      <circle
        cx="6"
        cy="19"
        r="2"
      />
      <circle
        cx="18"
        cy="7"
        r="2"
      />
      <path d="M6 7v10" />
      <path d="M18 9c0 5-6 4-12 8" />
    </svg>
  );
}
