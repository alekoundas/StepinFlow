import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { useFlowStore } from "./store/flow-store";
import { FlowViewToggleComponent } from "./components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "./components/FlowDataTableComponent";
import { FlowCardListComponent } from "./components/FlowCardListComponent";
import { useNavigate } from "react-router-dom";

export function FlowListPage() {
  const navigate = useNavigate();
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");
  const { flows, deleteFlow, fetchFlows } = useFlowStore();

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
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-4xl font-semibold">Flows</h1>
        <div className="flex gap-3">
          <FlowViewToggleComponent
            mode={viewMode}
            onChange={setViewMode}
          />
          <Button
            label="New Flow"
            icon="pi pi-plus"
            onClick={handleNew}
          />
        </div>
      </div>

      {viewMode === "table" ? (
        <FlowDataTableComponent
          flows={flows}
          onEdit={handleEdit}
          onClone={handleClone}
          onDelete={handleDelete}
        />
      ) : (
        <FlowCardListComponent
          flows={flows}
          onEdit={handleEdit}
          onClone={handleClone}
          onDelete={handleDelete}
        />
      )}
    </div>
  );
}
