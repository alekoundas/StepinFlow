import { useParams } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";
import { DataTreeComponent } from "@/shared/components/DataTreeComponent";

export function FlowTreeViewPage() {
  const { id } = useParams<{
    id?: string;
  }>();
  // const navigate = useNavigate();

  // const handleNew = () => navigate("/flows/new");
  return (
    <div className="m-4">
      {/* Title */}
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text="Flows"
          size="5xl"
          weight="bold"
        />
      </div>
      <div className="flex mt-6">
        <Card>
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
            onChange={seztViewMode}
            /> */}
          </div>
        </Card>
        <div>
          <DataTreeComponent flowId={id ? +id : -1} />
        </div>
      </div>
    </div>
  );
}
