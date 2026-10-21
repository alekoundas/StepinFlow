import { classNames } from "primereact/utils";

import IconComponent from "@/shared/components/IconComponent";
import {
  flowStepAccent,
  getFlowStepCatalogEntry,
  tint,
} from "@/shared/models/flow-step-catalog";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { StepIssues } from "@/features/flow/hooks/use-flow-validation";
import { buildFlowStepTreeDetail } from "@/features/flow-step/components/templates/data-tree/flow-step-tree-detail";

interface Props {
  treeNode: TreeNodeDto;
  issues?: StepIssues;
}

export function FlowStepTreeNodeComponent({ treeNode, issues }: Props) {
  const entry = getFlowStepCatalogEntry(treeNode.flowStepType);
  const detail = buildFlowStepTreeDetail(treeNode);
  const accent = flowStepAccent(treeNode.flowStepType);

  return (
    <div
      className="flex align-items-center gap-2 w-full min-w-0 py-1 pl-2 border-round-sm"
      style={{ borderLeft: `3px solid ${accent}`, background: tint(accent, 10) }}
    >
      {/* A template says more about what the step looks for than any icon can. */}
      {treeNode.detail?.thumbnail ? (
        <img
          className="flex-shrink-0 max-w-2rem max-h-2rem border-round-sm"
          src={`data:image/png;base64,${treeNode.detail.thumbnail}`}
          alt=""
        />
      ) : (
        <span
          className="flex align-items-center justify-content-center flex-shrink-0 w-2rem h-2rem border-round-sm"
          style={{ color: accent, background: tint(accent, 18) }}
        >
          <IconComponent
            name={entry?.iconName ?? "circle"}
            size="sm"
          />
        </span>
      )}

      <div className="flex align-items-center gap-2 flex-1 min-w-0">
        <span className="font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
          {treeNode.name}
        </span>

        {detail.text && (
          <span
            className="text-color-secondary text-xs white-space-nowrap overflow-hidden text-overflow-ellipsis min-w-0"
            title={detail.text}
          >
            {detail.text}
          </span>
        )}
      </div>

      {(issues || detail.chips.length > 0) && (
        <div className="flex align-items-center gap-1 flex-shrink-0">
          {issues && (
            <span
              className="flex align-items-center gap-1 text-xs px-2 py-1 border-round-sm white-space-nowrap"
              style={
                issues.errorCount > 0
                  ? { color: "var(--red-400)", background: tint("var(--red-400)", 15) }
                  : { color: "var(--yellow-400)", background: tint("var(--yellow-400)", 15) }
              }
              title={issues.messages.join("\n")}
            >
              <IconComponent
                name="exclamation-triangle"
                size="sm"
              />
              {issues.errorCount > 0 ? issues.errorCount : issues.warningCount}
            </span>
          )}

          {detail.chips.map((chip) => (
            <span
              key={chip.text}
              className={classNames(
                "text-xs px-2 py-1 border-round-sm white-space-nowrap",
                chip.isMuted && "surface-100 text-color-secondary",
              )}
              style={
                chip.isMuted
                  ? undefined
                  : { color: accent, background: tint(accent, 15) }
              }
            >
              {chip.text}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}
