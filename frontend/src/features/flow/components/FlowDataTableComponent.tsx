import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { useNavigate } from "react-router-dom";
import { DataTableComponent } from "@/shared/components/data-table/DataTableComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useFlowMutations } from "@/features/flow/hooks/use-flow";
import type { FlowDto } from "@/shared/models/database/flow-dto";

type Props = {
  className?: string;
};

export function FlowDataTableComponent({ className }: Props) {
  const navigate = useNavigate();
  const { deleteMutation } = useFlowMutations();

  const columns: DataTableColumnDto<FlowDto>[] = [
    { field: "name", header: "Name", sortable: true, filter: true },
    { field: "orderNumber", header: "Order", sortable: true },
    {
      field: "actions",
      header: "Actions",
      body: (row: FlowDto) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={(id) => navigate(`/flows/${id}/edit`)}
          onClone={(id) => navigate(`/flows/${id}/clone`)}
          onDelete={async (id) => {
            await deleteMutation.mutateAsync(id);

            // if (confirm("Delete this flow?")) backendApiService.Flow.delete(id); // or use store
          }}
        />
      ),
    },
  ];

  return (
    <div className={className}>
      <DataTableComponent
        columns={columns}
        queryKey={["flows", "list"]}
        queryFn={backendApiService.Flow.getLazy}
      />
    </div>
  );
}
