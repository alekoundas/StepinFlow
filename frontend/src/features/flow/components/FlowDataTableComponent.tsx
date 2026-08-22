import { useNavigate } from "react-router-dom";

import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { DataTableComponent } from "@/shared/components/data-table/DataTableComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import FlowHealthBadgeComponent from "@/features/flow/components/FlowHealthBadgeComponent";
import { useDeleteFlow } from "@/features/flow/hooks/use-delete-flow";
import { useFlowHealth } from "@/features/flow/hooks/use-flow-health";

type Props = {
  className?: string;

  /** Which side of the flag to list. The two pages never show each other. */
  isSubFlow: boolean;
};

/**
 * Everything about a flow, densely. The card view shows four of these; density is the reason a
 * table exists at all.
 */
export function FlowDataTableComponent({ className, isSubFlow }: Props) {
  const navigate = useNavigate();
  const deleteFlow = useDeleteFlow();
  const health = useFlowHealth();

  const open = (id: number) => navigate(`/workflow/${id}`);

  const columns: DataTableColumnDto<FlowDto>[] = [
    {
      field: "name",
      header: "Name",
      sortable: true,
      filter: true,
      body: (row: FlowDto) => (
        <div className="flex flex-column">
          <LabelComponent text={row.name} />
          {row.description && (
            <LabelComponent
              text={row.description}
              size="xs"
              color="secondary"
            />
          )}
        </div>
      ),
    },
    {
      field: "stepCount",
      header: "Steps",
      body: (row: FlowDto) => `${row.stepCount}`,
    },
    {
      field: "health",
      header: "Health",
      body: (row: FlowDto) => (
        <FlowHealthBadgeComponent
          health={health.get(row.id)}
          isEmpty={row.stepCount === 0}
        />
      ),
    },
    {
      field: "setup",
      header: "Areas / Points",
      body: (row: FlowDto) => `${row.areaCount} / ${row.pointCount}`,
    },
    // Only a sub-flow can be depended on, so the column would be a blank one anywhere else.
    ...(isSubFlow
      ? [
          {
            field: "callerCount",
            header: "Used by",
            body: (row: FlowDto) => `${row.callerCount}`,
          } as DataTableColumnDto<FlowDto>,
        ]
      : []),
    {
      field: "updatedOn",
      header: "Last edited",
      body: (row: FlowDto) => formatWhen(row.updatedOn ?? row.createdOn),
    },
    {
      field: "actions",
      header: "",
      body: (row: FlowDto) => (
        <ActionsMenuComponent
          id={row.id}
          onClone={(id) => navigate(`/flows/${id}/clone`)}
          onDelete={() => deleteFlow(row)}
          extraActions={[
            {
              label: "Settings",
              icon: "pi pi-cog",
              command: (id) => navigate(`/flows/${id}/edit`),
            },
          ]}
        />
      ),
    },
  ];

  return (
    <div className={className}>
      <DataTableComponent
        columns={columns}
        queryKey={["flows", "list", isSubFlow]}
        queryFn={(dto) => backendApiService.Flow.getLazy({ ...dto, isSubFlow })}
        onRowClick={(row) => open((row as FlowDto).id)}
      />
    </div>
  );
}

/** Dates are only ever glanced at here, so relative reads faster than a timestamp. */
const formatWhen = (value: string | undefined): string => {
  if (!value) return "";

  const days = Math.floor((Date.now() - new Date(value).getTime()) / 86_400_000);

  if (days <= 0) return "today";
  if (days === 1) return "yesterday";
  if (days < 30) return `${days} days ago`;

  return new Date(value).toLocaleDateString();
};
