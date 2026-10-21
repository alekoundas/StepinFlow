import { useNavigate, useParams } from "react-router-dom";
import LabelComponent from "@/shared/components/LabelComponent";
import { Button } from "primereact/button";
import { Splitter, SplitterPanel } from "primereact/splitter";
import { WorkflowContentComponent } from "@/features/workflow/components/WorkflowContentComponent";
import { CopiedStepBarComponent } from "@/features/workflow/components/CopiedStepBarComponent";
import { ScrollPanel } from "primereact/scrollpanel";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { useLayoutEffect, useState } from "react";
import { DataTreeComponent } from "@/shared/components/data-tree/DataTreeComponent";
import ExportFlowDialogComponent from "@/features/flow-script/components/ExportFlowDialogComponent";
import { useFlow } from "@/features/flow/hooks/use-flow";

export default function WorkflowPage() {
  const { id } = useParams<{
    id?: string; // Flow Id
  }>();
  const navigate = useNavigate();

  const flowId = id ? +id : undefined;

  const {
    setRootFlowId,
    setSelectedTreeNode,
    setSelectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
    setCopiedStep,
  } = useWorkflowStore();
  const { data: flow } = useFlow(flowId ?? null);
  const [isExporting, setIsExporting] = useState(false);

  // The selection lives in a store that outlives this page. A flow opened here, from the list or
  // by going back to another, starts with nothing selected and its own root, and leaving lets go
  // of both. Before paint, so the panel never shows a frame of the flow that was open before. A
  // copy goes too: it pastes only into the flow it came from.
  useLayoutEffect(() => {
    const clear = () => {
      setSelectedTreeNode(undefined);
      setSelectedFlowStepTypeToAdd(undefined);
      setTreeRefreshTrigger(null);
      setCopiedStep(undefined);
    };

    clear();
    setRootFlowId(flowId);

    return () => {
      clear();
      setRootFlowId(undefined);
    };
  }, [
    flowId,
    setRootFlowId,
    setSelectedTreeNode,
    setSelectedFlowStepTypeToAdd,
    setTreeRefreshTrigger,
    setCopiedStep,
  ]);

  return (
    <div className="flex flex-column m-4 mr-3">
      {/* Title */}
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text="Flows"
          size="5xl"
          weight="bold"
        />

        <div className="flex gap-2">
          <Button
            label="Export"
            icon="pi pi-file-export"
            onClick={() => setIsExporting(true)}
            disabled={!flow}
            severity="secondary"
            outlined
          />
          <Button
            label="Run"
            icon="pi pi-play"
            onClick={() => navigate(`/execution/${id}`)}
            disabled={!id}
          />
        </div>
      </div>

      {flow && (
        <ExportFlowDialogComponent
          flowId={flow.id}
          flowName={flow.name}
          visible={isExporting}
          onHide={() => setIsExporting(false)}
        />
      )}

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
            <ScrollPanel style={{ width: "100%", height: "100%" }}>
              <WorkflowContentComponent />
            </ScrollPanel>
          </SplitterPanel>

          <SplitterPanel
            size={70} // the rest
            minSize={15}
            className="flex flex-column "
            style={{ overflow: "hidden" }}
          >
            <CopiedStepBarComponent />
            <ScrollPanel
              className="flex-1"
              style={{ minHeight: 0 }}
            >
              {/* Keyed by the flow, so its open rows and loaded branches start over with each one. */}
              <DataTreeComponent
                key={flowId ?? -1}
                flowId={flowId ?? -1}
              />
            </ScrollPanel>
          </SplitterPanel>
        </Splitter>
      </div>
    </div>
  );
}
