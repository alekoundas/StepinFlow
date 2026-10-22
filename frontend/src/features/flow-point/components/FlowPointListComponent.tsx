import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import {
  WorkflowPartListComponent,
  type WorkflowPartSelection,
} from "@/features/workflow/components/WorkflowPartListComponent";

interface Props {
  points: FlowPointDto[];
  areas: FlowAreaDto[];
  uses: FlowStepUseDto[] | undefined;
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

/** The Locations tab's list: each point, and what it is measured from. */
export function FlowPointListComponent({ points, areas, uses, selected, onSelect }: Props) {
  const items = points.map((point) => ({
    id: point.id,
    icon: "map-marker",
    name: point.name,
    detail: describePoint(point, areas),
    usedCount: (uses ?? []).filter((x) => x.flowPointId === point.id || x.flowPointEndId === point.id).length,
  }));

  return (
    <WorkflowPartListComponent
      note="Points a cursor step can move to, measured from an area."
      addLabel="Add location"
      emptyText="No locations yet."
      items={items}
      selected={selected}
      onSelect={onSelect}
    />
  );
}

// Where a point is, said the way the location dropdowns say it.
const describePoint = (point: FlowPointDto, areas: FlowAreaDto[]): string => {
  const area = areas.find((x) => x.id === point.flowAreaId);
  if (!area) return `${point.locationX}, ${point.locationY} on screen`;

  if (point.offsetMode === AreaSizingModeEnum.RATIO)
    return `${percent(point.ratioX)}, ${percent(point.ratioY)} of ${area.name}`;

  return `${point.locationX}, ${point.locationY} px in ${area.name}`;
};

const percent = (ratio: number) => `${Math.round(ratio * 1000) / 10}%`;
