import { useState } from "react";
import type { CSSProperties } from "react";

import LabelComponent from "@/shared/components/LabelComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import type { RepositoryCommitDto } from "@/shared/models/repository-dtos";
import TemplateImageChangesComponent from "@/features/repository/components/TemplateImageChangesComponent";
import {
  useFlowRepositoryCommit,
  useFlowRepositoryHistory,
} from "@/features/repository/hooks/use-repository";
import { MONO, formatWhen } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  flowId: number;
}

/** The commits that changed the flow, newest first, and what the picked one changed. */
export default function FlowHistoryComponent({ flowId }: Props) {
  const { data: commits, isLoading, error } = useFlowRepositoryHistory(flowId);
  const [picked, setPicked] = useState<string | null>(null);

  if (isLoading)
    return (
      <LabelComponent
        text="Reading the history..."
        color="secondary"
      />
    );

  if (error)
    return (
      <LabelComponent
        text={error instanceof Error ? error.message : String(error)}
        color="error"
      />
    );

  if (!commits || commits.length === 0)
    return (
      <span
        className="text-sm"
        style={MUTED}
      >
        No commit has this flow yet. Commit it to start its history.
      </span>
    );

  const sha = picked ?? commits[0].sha;

  return (
    <div className="flex flex-wrap align-items-start gap-3">
      <ol
        aria-label="Commits"
        className="list-none m-0 p-0 flex flex-column gap-1"
        style={{ flex: "1 1 300px", minWidth: 0 }}
      >
        {commits.map((commit) => (
          <li key={commit.sha}>
            <CommitRowComponent
              commit={commit}
              isPicked={commit.sha === sha}
              onPick={() => setPicked(commit.sha)}
            />
          </li>
        ))}
      </ol>

      <div style={{ flex: "999 1 560px", minWidth: 0 }}>
        <CommitDetailComponent
          flowId={flowId}
          sha={sha}
        />
      </div>
    </div>
  );
}

interface RowProps {
  commit: RepositoryCommitDto;
  isPicked: boolean;
  onPick: () => void;
}

function CommitRowComponent({ commit, isPicked, onPick }: RowProps) {
  return (
    <button
      type="button"
      onClick={onPick}
      aria-pressed={isPicked}
      className="w-full flex align-items-start gap-3 p-2 border-round-md text-left cursor-pointer"
      style={{
        border: `1px solid ${isPicked ? "var(--primary-color)" : "transparent"}`,
        background: isPicked ? "color-mix(in srgb, var(--primary-color) 10%, transparent)" : "transparent",
        color: "inherit",
        font: "inherit",
      }}
    >
      <span
        className="border-circle flex-none mt-1"
        style={{ width: 10, height: 10, border: "2px solid var(--primary-color)" }}
      />
      <span className="flex flex-column gap-1 flex-1 min-w-0">
        <span className="font-medium">{commit.message.split("\n")[0]}</span>
        <span
          className="text-xs"
          style={MUTED}
        >
          {commit.authorName} · {formatWhen(commit.when)}
        </span>
      </span>
      <span
        className="text-xs flex-none"
        style={{ ...MUTED, fontFamily: MONO }}
      >
        {commit.shortSha}
      </span>
    </button>
  );
}

interface DetailProps {
  flowId: number;
  sha: string;
}

function CommitDetailComponent({ flowId, sha }: DetailProps) {
  const { data: detail, isLoading, error } = useFlowRepositoryCommit(flowId, sha);

  if (isLoading)
    return (
      <LabelComponent
        text="Reading the commit..."
        color="secondary"
      />
    );

  if (error)
    return (
      <LabelComponent
        text={error instanceof Error ? error.message : String(error)}
        color="error"
      />
    );

  if (!detail) return null;

  const before = detail.scriptBefore ?? "";
  const after = detail.scriptAfter ?? "";

  return (
    <div className="flex flex-column gap-3">
      <div className="flex flex-column gap-1">
        <span className="font-semibold">{detail.commit.message}</span>
        <span
          className="text-xs"
          style={MUTED}
        >
          {detail.commit.authorName} · {formatWhen(detail.commit.when)} ·{" "}
          <span style={{ fontFamily: MONO }}>{detail.commit.shortSha}</span>
        </span>
      </div>

      {before !== after ? (
        <ScriptViewComponent
          script={after}
          original={before}
          collapseUnchanged
          maxHeight={2000}
        />
      ) : (
        <span
          className="text-sm"
          style={MUTED}
        >
          The script itself did not change in this commit.
        </span>
      )}

      <TemplateImageChangesComponent images={detail.images} />
    </div>
  );
}
