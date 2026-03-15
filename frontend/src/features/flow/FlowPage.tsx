import { useState } from "react";
import { Button } from "primereact/button";
import { useFlowStore } from "./store/flow-store";
import { FlowViewToggleComponent } from "./components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "./components/FlowDataTableComponent";
import { FlowCardListComponent } from "./components/FlowCardListComponent";
import { FlowFormComponent } from "./FlowFormComponent";
import { FormMode } from "../../models/enums/form-mode-enum";

export default function FlowPage() {
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");
  const [showForm, setShowForm] = useState(false);
  const [formMode, setFormMode] = useState<FormMode>(FormMode.ADD);
  const [selectedFlowId, setSelectedFlowId] = useState<number | null>(null);

  const { flows, deleteFlow } = useFlowStore();

  const handleEdit = (id: number) => {
    setSelectedFlowId(id);
    setFormMode(FormMode.EDIT);
    setShowForm(true);
  };

  const handleClone = (id: number) => {
    setSelectedFlowId(id);
    setFormMode(FormMode.CLONE);
    setShowForm(true);
  };

  const handleDelete = (id: number) => {
    if (confirm("Delete this flow?")) {
      deleteFlow(id);
    }
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
            onClick={() => {
              setFormMode(FormMode.ADD);
              setSelectedFlowId(null);
              setShowForm(true);
            }}
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

      <FlowFormComponent
        visible={showForm}
        mode={formMode}
        flowId={selectedFlowId}
        onHide={() => setShowForm(false)}
      />
    </div>
  );
}
