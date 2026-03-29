import { FormMode } from "@/shared/enums/form-mode-enum";
import { Button } from "primereact/button";
import { useNavigate, useParams } from "react-router-dom";
import { FlowFormComponent } from "@/features/flow/components/form/FlowFormComponent";
import { useFlowMutations } from "@/features/flow/hooks/use-flow";
import { FlowDto } from "@/shared/models/database/flow-dto";

export function FlowFormPage() {
  const { id, formMode = FormMode.ADD } = useParams<{
    id?: string;
    formMode: FormMode;
  }>();
  const navigate = useNavigate();
  // const { isLoading } = useFlow(id ? +id : -1);
  const { createMutation, updateMutation } = useFlowMutations();

  // const flow = id ? flows.find((f) => f.id === Number(id)) : null;
  const flow = null;

  const handleSubmit = async (data: FlowDto) => {
    if (formMode === FormMode.ADD || formMode === FormMode.CLONE) {
      await createMutation.mutateAsync(data);
    } else if (id) {
      await updateMutation.mutateAsync({ ...data, id: +id });
    }
    navigate("/flows");
  };

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
        defaultValues={flow ?? new FlowDto()}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/flows")}
      />
    </div>
  );
}
