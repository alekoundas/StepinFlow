import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import {
  WorkflowPartListComponent,
  type WorkflowPartItem,
  type WorkflowPartSelection,
} from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  areas: FlowAreaDto[];
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

/** The Areas tab's list: each window, monitor or tab, with the regions inside it under it. */
export function FlowAreaListComponent({ areas, uses, selected, onSelect }: Props) {
  const usedCount = (area: FlowAreaDto) =>
    (uses ?? []).filter((x) => x.flowAreaId === area.id).length;

  const item = (area: FlowAreaDto, depth: number): WorkflowPartItem => ({
    id: area.id,
    icon: ICONS[area.type] ?? "stop",
    name: area.name,
    detail: describe(area, areas),
    usedCount: usedCount(area),
    chip: area.isMain ? "Main" : undefined,
    depth,
  });

  // A region whose window was deleted has no parent any more, so it is listed as a root.
  const isRoot = (area: FlowAreaDto) =>
    !area.parentFlowAreaId || !areas.some((x) => x.id === area.parentFlowAreaId);

  const items = areas
    .filter(isRoot)
    .flatMap((root) => [
      item(root, 0),
      ...areas.filter((x) => x.parentFlowAreaId === root.id).map((x) => item(x, 1)),
    ]);

  return (
    <WorkflowPartListComponent
      note="Where steps look. A region sits inside a window and follows it."
      addLabel="Add area"
      emptyText="No areas yet."
      items={items}
      selected={selected}
      onSelect={onSelect}
    />
  );
}

const ICONS: Record<FlowAreaTypeEnum, string> = {
  [FlowAreaTypeEnum.APPLICATION]: "window-maximize",
  [FlowAreaTypeEnum.BROWSER_TAB]: "globe",
  [FlowAreaTypeEnum.MONITOR]: "desktop",
  [FlowAreaTypeEnum.CUSTOM]: "stop",
};

const percent = (ratio: number) => `${Math.round(ratio * 100)}%`;

const describe = (area: FlowAreaDto, areas: FlowAreaDto[]): string => {
  if (area.type === FlowAreaTypeEnum.MONITOR) return area.monitorDeviceName || "Primary monitor";

  if (area.type === FlowAreaTypeEnum.BROWSER_TAB)
    return [area.processName, area.tabMatchValue].filter(Boolean).join(" · ") || "Browser tab";

  if (area.type === FlowAreaTypeEnum.APPLICATION)
    return [area.processName, area.titlePattern && `title ${area.titlePattern}`].filter(Boolean).join(" · ") || "Any window";

  const parent = areas.find((x) => x.id === area.parentFlowAreaId);
  const where = parent ? `inside ${parent.name} · ` : "";

  if (area.sizingMode === AreaSizingModeEnum.RATIO)
    return `${where}${percent(area.ratioX)}, ${percent(area.ratioY)} · ${percent(area.ratioWidth)} × ${percent(area.ratioHeight)}`;

  return `${where}${area.locationX}, ${area.locationY} · ${area.width} × ${area.height} px`;
};
