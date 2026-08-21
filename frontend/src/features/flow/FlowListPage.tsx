import { useState } from "react";
import { useNavigate } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { FlowViewToggleComponent } from "@/features/flow/components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "@/features/flow/components/FlowDataTableComponent";
import { FlowDataGridComponent } from "@/features/flow/components/FlowDataGridComponent";
import { useWizardStore } from "@/features/wizard/store/wizard-store";

export default function FlowListPage() {
  const navigate = useNavigate();
  const { setTarget, setDraft } = useWizardStore();
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");

  const handleNew = () => navigate("/flows/new");

  // The recorder creates the flow on save, so nothing exists yet to navigate to first.
  const handleRecord = () => {
    setDraft(undefined);
    setTarget({ targetIndex: 0, newFlowName: "Recorded flow" });
    navigate("/record");
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

        <div className="flex gap-2">
          <Button
            label="Record a Flow"
            icon="pi pi-circle-fill"
            onClick={handleRecord}
            className="p-button-outlined"
            tooltip="Do the task once and let the steps be written for you"
            tooltipOptions={{ position: "bottom" }}
          />
          <Button
            label="New Flow"
            icon="pi pi-plus"
            onClick={handleNew}
          />
        </div>
      </div>

      <Card className="mt-6">
        <div className="flex flex-wrap justify-content-between items-center">
          <div className="flex flex-column">
            <LabelComponent
              text="Available Flows"
              size="lg"
              weight="bold"
            />
            <LabelComponent
              text="Available Flows"
              size="xs"
            />
          </div>

          <FlowViewToggleComponent
            mode={viewMode}
            onChange={setViewMode}
          />
        </div>

        {viewMode === "table" && <FlowDataTableComponent className="mt-4" />}
      </Card>
      {viewMode === "cards" && <FlowDataGridComponent className="mt-4" />}
    </div>
  );
}
