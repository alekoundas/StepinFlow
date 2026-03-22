import { useNavigate, useParams } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { FlowViewToggleComponent } from "@/features/flow/components/FlowViewToggleComponent";
import { FlowDataTableComponent } from "@/features/flow/components/FlowDataTableComponent";
import { FlowDataGridComponent } from "@/features/flow/components/FlowDataGridComponent";

export function FlowPage() {
  const { id } = useParams<{
    id?: string;
  }>();
  const navigate = useNavigate();

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

          {/* <FlowViewToggleComponent
            mode={viewMode}
            onChange={setViewMode}
          /> */}
        </div>
      </Card>
    </div>
  );
}
