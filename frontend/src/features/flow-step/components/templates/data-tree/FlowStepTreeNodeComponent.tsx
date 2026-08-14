import { classNames } from "primereact/utils";

import IconComponent from "@/shared/components/IconComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import {
  FlowStepGroupEnum,
  getFlowStepCatalogEntry,
} from "@/shared/models/flow-step-catalog";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import { buildFlowStepTreeDetail } from "@/features/flow-step/components/templates/data-tree/flow-step-tree-detail";

interface Props {
  treeNode: TreeNodeDto;
}

/**
 * A colour per group, so a glance says what kind of thing a step does.
 *
 * Only the accent is picked here and the tints are mixed from it. PrimeFlex's bg-{colour}-50
 * classes cannot be used: the palette is not inverted for dark themes, so bg-blue-50 is near
 * white on soho-dark. Mixing into transparent tints whatever surface is actually behind, which
 * lands correctly on either.
 *
 * No group uses green or red. Those mean Success and Failure.
 */
const GROUP_ACCENTS: Record<FlowStepGroupEnum, string> = {
  CONTROL: "var(--bluegray-400)",
  INPUT: "var(--blue-400)",
  WINDOW: "var(--purple-400)",
  PERCEPTION: "var(--orange-400)",
  SYSTEM: "var(--teal-400)",
  BRANCH: "var(--bluegray-400)",
};

/** The two branches carry meaning rather than a category, so they get the semantic colours. */
const TYPE_ACCENTS: Partial<Record<FlowStepTypeEnum, string>> = {
  [FlowStepTypeEnum.SUCCESS]: "var(--green-400)",
  [FlowStepTypeEnum.FAILURE]: "var(--red-400)",
};

const tint = (accent: string, percent: number) =>
  `color-mix(in srgb, ${accent} ${percent}%, transparent)`;

export function FlowStepTreeNodeComponent({ treeNode }: Props) {
  const entry = getFlowStepCatalogEntry(treeNode.flowStepType);
  const detail = buildFlowStepTreeDetail(treeNode);

  const group = entry?.group ?? FlowStepGroupEnum.CONTROL;
  const accent =
    (treeNode.flowStepType && TYPE_ACCENTS[treeNode.flowStepType]) ??
    GROUP_ACCENTS[group];

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

      {detail.chips.length > 0 && (
        <div className="flex align-items-center gap-1 flex-shrink-0">
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
