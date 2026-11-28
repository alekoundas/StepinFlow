import { RepositoryHooks } from "@/features/repository/hooks/use-repository";
import { RepositoryFormat } from "@/features/repository/repository-format";

interface Props {
  flowId: number;
}

/** "3 changes" beside a flow that has been saved since its last commit. Nothing otherwise. */
export default function UncommittedBadgeComponent({ flowId }: Props) {
  const { data: status } = RepositoryHooks.useRepositoryStatus();

  const count = status?.flows.find((x) => x.flowId === flowId)?.changeCount ?? 0;
  if (count === 0) return null;

  return (
    <span
      title="Saved, not committed yet"
      className="inline-flex align-items-center gap-1 text-xs white-space-nowrap"
      style={{ color: RepositoryFormat.UNCOMMITTED_COLOR }}
    >
      <span
        className="border-circle"
        style={{ width: 7, height: 7, background: RepositoryFormat.UNCOMMITTED_COLOR }}
      />
      {RepositoryFormat.plural(count, "change")}
    </span>
  );
}
