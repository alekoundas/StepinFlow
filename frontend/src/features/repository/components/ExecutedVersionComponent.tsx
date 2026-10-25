import { MONO, UNCOMMITTED_COLOR } from "@/features/repository/repository-format";

interface Props {
  commitSha?: string | null;
  hasUncommittedChanges?: boolean | null;
}

/** The commit a run executed at, and a mark when it executed changes not committed yet. Nothing for a run from before the repository. */
export default function ExecutedVersionComponent({ commitSha, hasUncommittedChanges }: Props) {
  if (!commitSha && !hasUncommittedChanges) return null;

  return (
    <span className="inline-flex align-items-center gap-2 text-xs white-space-nowrap">
      {commitSha ? (
        <span
          title={`Executed at commit ${commitSha}`}
          className="text-color-secondary"
          style={{ fontFamily: MONO }}
        >
          {commitSha.slice(0, 7)}
        </span>
      ) : null}

      {hasUncommittedChanges ? (
        <span
          title="The flow, or a sub-flow it calls, had changes not committed yet. The script it executed is kept with the run."
          className="inline-flex align-items-center gap-1"
          style={{ color: UNCOMMITTED_COLOR }}
        >
          <span
            className="border-circle"
            style={{ width: 7, height: 7, background: UNCOMMITTED_COLOR }}
          />
          uncommitted
        </span>
      ) : null}
    </span>
  );
}
