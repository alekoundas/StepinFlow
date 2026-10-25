import { useNavigate, useParams } from "react-router-dom";
import { useLayoutEffect, useState } from "react";

import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { WorkflowHeaderComponent } from "@/features/workflow/components/WorkflowHeaderComponent";
import { WorkflowEditorComponent } from "@/features/workflow/components/WorkflowEditorComponent";
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
    setActiveTab,
    setScriptView,
  } = useWorkflowStore();
  const { data: flow } = useFlow(flowId ?? null);
  const [isExporting, setIsExporting] = useState(false);

  // The selection lives in a store that outlives this page. A flow opened here, from the list or
  // by going back to another, starts with nothing selected, on its Flow tab and with its own
  // root, and leaving lets go of all of it. Before paint, so the panel never shows a frame of the
  // flow that was open before. A copy goes too: it pastes only into the flow it came from.
  useLayoutEffect(() => {
    const clear = () => {
      setSelectedTreeNode(undefined);
      setSelectedFlowStepTypeToAdd(undefined);
      setTreeRefreshTrigger(null);
      setCopiedStep(undefined);
      setActiveTab("flow");
      setScriptView("script");
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
    setActiveTab,
    setScriptView,
  ]);

  return (
    <div
      className="flex flex-column"
      style={{ height: "100vh" }}
    >
      <WorkflowHeaderComponent
        flow={flow}
        onExport={() => setIsExporting(true)}
        onExecute={() => navigate(`/execution/${id}`)}
      />

      {flow && (
        <ExportFlowDialogComponent
          flowId={flow.id}
          flowName={flow.name}
          visible={isExporting}
          onHide={() => setIsExporting(false)}
        />
      )}

      {/* Keyed by the flow, so its open rows, loaded branches and picked parts start over with each one. */}
      {flowId !== undefined && (
        <WorkflowEditorComponent
          key={flowId}
          flowId={flowId}
          flow={flow}
        />
      )}
    </div>
  );
}
