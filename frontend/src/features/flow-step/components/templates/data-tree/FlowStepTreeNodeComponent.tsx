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

const THUMBNAIL_BOX = { width: "2.5rem", height: "2rem" };

export function FlowStepTreeNodeComponent({ treeNode, issues }: Props) {
  const entry = getFlowStepCatalogEntry(treeNode.flowStepType);
  const detail = buildFlowStepTreeDetail(treeNode);
  const accent = flowStepAccent(treeNode.flowStepType);
  const thumbnail = treeNode.detail?.thumbnail;
  const moreTemplates = (treeNode.detail?.templateCount ?? 0) - 1;
  const badge = issues ? badgeOf(issues) : null;

  return (
    <div
      className="flex align-items-center gap-2 w-full min-w-0 py-1 pl-2 border-round-sm"
      style={{ borderLeft: `3px solid ${accent}`, background: tint(accent, 10) }}
    >
      {/* A template says more about what the step looks for than any icon can. The others stack
          behind it, and the step's icon stays on its corner. */}
      {thumbnail ? (
        <span className="relative flex flex-shrink-0 mr-1">
          {/* The theme's variables: PrimeFlex 4's surface classes come out transparent (TODO.md). */}
          {moreTemplates > 0 && (
            <span
              className="absolute border-round-sm opacity-40"
              style={{ ...THUMBNAIL_BOX, left: 3, top: -3, background: "var(--surface-400)" }}
            />
          )}
          <span
            className="relative flex align-items-center justify-content-center border-round-sm border-1 overflow-hidden"
            style={{ ...THUMBNAIL_BOX, borderColor: "var(--surface-border)", background: "var(--surface-ground)" }}
          >
            <img
              className="max-w-full max-h-full"
              src={`data:image/png;base64,${thumbnail}`}
              alt=""
            />
          </span>
          <span
            className="absolute flex align-items-center justify-content-center border-round-sm"
            style={{
              right: -5,
              bottom: -4,
              width: 14,
              height: 14,
              color: accent,
              background: "var(--surface-card)",
              boxShadow: `0 0 0 1px ${tint(accent, 40)}`,
            }}
          >
            <IconComponent
              name={entry?.iconName ?? "circle"}
              size="xs"
            />
          </span>
          {moreTemplates > 0 && (
            <span
              className="absolute flex align-items-center justify-content-center font-bold border-round-3xl"
              style={{
                right: -7,
                top: -6,
                minWidth: 16,
                height: 14,
                padding: "0 3px",
                fontSize: 9,
                color: "var(--primary-color-text)",
                background: "var(--primary-color)",
              }}
              title={`${moreTemplates} more ${moreTemplates === 1 ? "template" : "templates"}`}
            >
              +{moreTemplates}
            </span>
          )}
        </span>
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
          {issues && badge && (
            <span
              className="flex align-items-center gap-1 text-xs px-2 py-1 border-round-sm white-space-nowrap"
              style={{ color: badge.color, background: tint(badge.color, 15) }}
              title={issues.messages.join("\n")}
            >
              <IconComponent
                name={badge.icon}
                size="sm"
              />
              {badge.count}
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

// The worst there is decides the badge: an error, then a warning, then a note, which is quieter.
function badgeOf(issues: StepIssues): { color: string; icon: string; count: number } {
  if (issues.errorCount > 0) return { color: "var(--red-400)", icon: "exclamation-triangle", count: issues.errorCount };
  if (issues.warningCount > 0)
    return { color: "var(--yellow-400)", icon: "exclamation-triangle", count: issues.warningCount };

  return { color: "var(--text-color-secondary)", icon: "info-circle", count: issues.noteCount };
}
