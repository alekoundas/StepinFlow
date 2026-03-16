import { Button } from "primereact/button";
import { useFlowStore } from "./store/flow-store";
import { FormMode } from "@/models/enums/form-mode-enum";
import { FlowFormComponent } from "@/features/flow/components/FlowFormComponent";
import { useNavigate, useParams } from "react-router-dom";
import { useEffect } from "react";

export function FlowFormPage() {
  const { id, formMode = FormMode.ADD } = useParams<{
    id?: string;
    formMode: FormMode;
  }>();
  const navigate = useNavigate();
  const { flows, loading, createFlow, updateFlow } = useFlowStore();
  const flow = id ? flows.find((f) => f.id === Number(id)) : null;

  const handleSubmit = async (data: any) => {
    if (formMode === FormMode.ADD || formMode === FormMode.CLONE) {
      await createFlow(data);
    } else if (id) {
      await updateFlow(Number(id), { ...data, id: Number(id) });
    }
    navigate("/flows");
  };

  useEffect(() => {
    if (id && !flow) {
      useFlowStore.getState().fetchFlows();
    }
  }, [id]);

  return (
    <div className="p-6">
      <div className="flex items-center gap-4 mb-6">
        <Button
          icon="pi pi-arrow-left"
          text
          onClick={() => navigate("/flows")}
        />
        <h1 className="text-3xl">
          {formMode === FormMode.ADD
            ? "New Flow"
            : formMode === FormMode.EDIT
              ? "Edit Flow"
              : "View Flow"}
        </h1>
      </div>

      <FlowFormComponent
        formMode={formMode}
        initialData={flow ?? {}}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/flows")}
        loading={loading}
      />
    </div>
  );
}
