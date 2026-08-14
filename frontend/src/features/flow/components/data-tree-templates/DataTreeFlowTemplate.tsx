import IconComponent from "@/shared/components/IconComponent";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  treeNode: TreeNodeDto;
}

const ACCENT = "var(--primary-color)";

export function DataTreeFlowTemplate({ treeNode }: Props) {
  return (
    <div
      className="flex align-items-center gap-2 w-full min-w-0 py-1 pl-2 border-round-sm"
      style={{
        borderLeft: `3px solid ${ACCENT}`,
        background: `color-mix(in srgb, ${ACCENT} 10%, transparent)`,
      }}
    >
      <span
        className="flex align-items-center justify-content-center flex-shrink-0 w-2rem h-2rem border-round-sm"
        style={{
          color: ACCENT,
          background: `color-mix(in srgb, ${ACCENT} 18%, transparent)`,
        }}
      >
        <IconComponent
          name="share-alt"
          size="sm"
        />
      </span>
      <span className="font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
        {treeNode.name}
      </span>
    </div>
  );
}
