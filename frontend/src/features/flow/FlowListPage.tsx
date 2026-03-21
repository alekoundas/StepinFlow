import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { useFlowStore } from "./store/flow-store";
import { FlowViewToggleComponent } from "./components/FlowViewToggleComponent";
import { useNavigate } from "react-router-dom";
import { DataTableComponent } from "@/components/datatable/DataTableComponent";
import type { DataTableColumnDto } from "@/shared/models/data-table/datatable-column-dto";
import { backendApiService } from "@/services/backend-api-service";
import { FlowActionsMenuComponent } from "@/features/flow/components/FlowActionsMenuComponent";
import type { FlowDto } from "@/shared/models/flow/flow-dto";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";

export function FlowListPage() {
  const navigate = useNavigate();
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");
  const { deleteFlow, fetchFlows } = useFlowStore();

  const columns: DataTableColumnDto<FlowDto>[] = [
    { field: "name", header: "Name", sortable: true, filter: true },
    { field: "orderNumber", header: "Order", sortable: true },
  ];

  const actionsColumn: DataTableColumnDto<FlowDto> = {
    field: "actions",
    header: "Actions",
    body: (row: FlowDto) => (
      <FlowActionsMenuComponent
        flowId={row.id}
        onEdit={(id) => navigate(`/flows/${id}/edit`)}
        onClone={(id) => navigate(`/flows/${id}/clone`)}
        onDelete={(_id) => {
          // if (confirm("Delete this flow?")) backendApiService.Flow.delete(id); // or use store
        }}
      />
    ),
  };

  useEffect(() => {
    fetchFlows();
  }, []);

  const handleNew = () => navigate("/flows/new");
  const handleEdit = (id: number) => navigate(`/flows/${id}/edit`);
  const handleClone = (id: number) => navigate(`/flows/${id}/clone`);
  const handleDelete = (id: number) => {
    if (confirm("Delete?")) deleteFlow(id);
  };
  return (
    <div className="m-4">
      {/* Title */}
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text="Flows"
          size="5xl"
          weight="bold"
        />

        <Button
          label="New Flow"
          icon="pi pi-plus"
          onClick={handleNew}
        />
      </div>

      <Card className="mt-6">
        <div className="flex flex-wrap justify-content-between items-center">
          <div className="flex flex-column">
            <LabelComponent
              text="Available Flows"
              size="lg"
              weight="semibold"
            />
            <LabelComponent
              text="Available Flows"
              size="lg"
              weight="bold"
            />
            <LabelComponent
              text="Available Flows"
              size="sm"
            />
          </div>

          <FlowViewToggleComponent
            mode={viewMode}
            onChange={setViewMode}
          />
        </div>

        <div className="mt-4">
          {viewMode === "table" ? (
            <DataTableComponent
              columns={columns}
              loadData={backendApiService.Flow.getDataTable}
              actionsColumn={actionsColumn}
            />
          ) : (
            <DataTableComponent
              columns={columns}
              loadData={backendApiService.Flow.getDataTable}
              actionsColumn={actionsColumn}
            />
            // <FlowDataTableComponent
            //   flows={flows}
            //   onEdit={handleEdit}
            //   onClone={handleClone}
            //   onDelete={handleDelete}
            // />
            // <FlowCardListComponent
            //   flows={flows}
            //   onEdit={handleEdit}
            //   onClone={handleClone}
            //   onDelete={handleDelete}
            // />
          )}
        </div>
      </Card>
    </div>
  );
}
