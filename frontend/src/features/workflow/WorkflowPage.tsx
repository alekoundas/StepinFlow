import { useParams } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Splitter, SplitterPanel } from "primereact/splitter";
import { WorkflowContentComponent } from "@/features/workflow/components/WorkflowContentComponent";
import { ScrollPanel } from "primereact/scrollpanel";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { useEffect } from "react";
import { DataTreeComponent } from "@/shared/components/data-tree/DataTreeComponent";

export default function WorkflowPage() {
  const { id } = useParams<{
    id?: string; // Flow Id
  }>();
  // const navigate = useNavigate();

  // const handleNew = () => navigate("/flows/new");
  const { setRootFlowId } = useWorkflowStore();
  useEffect(() => {
    setRootFlowId(id ? +id : undefined);
  }, []);
  return (
    <div className="flex flex-column m-4 mr-3">
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
            style={{ overflow: "hidden" }}
            className="flex flex-column m-3"
          >
            {/* <Card className="h-full"> */}
            {/* <ScrollPanel className="h-full"> */}
            {/* <div className="flex flex-column gap-2 "> */}
            <ScrollPanel style={{ width: "100%", height: "100%" }}>
              <WorkflowContentComponent />
            </ScrollPanel>
            {/* </div> */}

            {/* If you have scrollable list/table inside → add flex-1 overflow-auto */}
            {/* e.g. <DataTable className="flex-1" ... /> */}
            {/* </Card> */}
            {/* </ScrollPanel> */}
          </SplitterPanel>

          <SplitterPanel
            size={70} // the rest
            minSize={15}
            className="flex flex-column "
            style={{ overflow: "hidden" }}
          >
            <ScrollPanel className="h-full">
              <DataTreeComponent flowId={id ? +id : -1} />
            </ScrollPanel>
          </SplitterPanel>
        </Splitter>
      </div>
    </div>
  );
}
