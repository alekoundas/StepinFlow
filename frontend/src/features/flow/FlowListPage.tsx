import { useState } from "react";
import { useNavigate } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { FlowViewToggleComponent } from "@/features/flow/components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "@/features/flow/components/FlowDataTableComponent";
import { FlowDataGridComponent } from "@/features/flow/components/FlowDataGridComponent";

export default function FlowListPage() {
  const navigate = useNavigate();
  const [viewMode, setViewMode] = useState<"table" | "cards">("table");

  const handleNew = () => navigate("/flows/new");
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
