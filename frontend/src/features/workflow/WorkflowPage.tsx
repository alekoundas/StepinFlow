import { useParams } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { DataTreeComponent } from "@/shared/components/data-tree/DataTreeComponent";
import { Splitter, SplitterPanel } from "primereact/splitter";
import { WorkflowContentComponent } from "@/features/workflow/components/WorkflowContentComponent";

export function WorkflowPage() {
  const { id } = useParams<{
    id?: string;
  }>();
  // const navigate = useNavigate();

  // const handleNew = () => navigate("/flows/new");
  return (
    <div className="m-4 mr-3">
      {/* Title */}
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text="Flows"
          size="5xl"
          weight="bold"
        />
      </div>

      <div
        className="mt-6"
        style={{ height: "70vh" }}
      >
        <Splitter
          stateKey="flow-tree-splitter" //  remembers user choice in localStorage
          stateStorage="local"
          layout="horizontal"
          gutterSize={10} // thickness of draggable bar
          className="  h-full"
        >
          <SplitterPanel
            size={30} // default size
            minSize={15} // can't shrink below 15%
            className="flex flex-column"
          >
            {/* <Card className="h-full"> */}
            <div className="h-full flex flex-column gap-2 ">
              <LabelComponent
                text="Available Flows"
                size="lg"
                weight="bold"
              />
              <LabelComponent
                text="Available Flows"
                size="xs"
              />
              <WorkflowContentComponent />
            </div>

            {/* If you have scrollable list/table inside → add flex-1 overflow-auto */}
            {/* e.g. <DataTable className="flex-1" ... /> */}
            {/* </Card> */}
          </SplitterPanel>

          <SplitterPanel
            size={70} // the rest
            minSize={25}
            className="flex flex-column"
          >
            <div className="h-full">
              <DataTreeComponent flowId={id ? +id : -1} />
            </div>
          </SplitterPanel>
        </Splitter>
      </div>
    </div>
  );
}
