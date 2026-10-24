import { useState } from "react";
import { classNames } from "primereact/utils";
import { ScrollPanel } from "primereact/scrollpanel";
import { Splitter, SplitterPanel } from "primereact/splitter";

import type { FlowDto } from "@/shared/models/database/flow-dto";
import { DataTreeComponent } from "@/shared/components/data-tree/DataTreeComponent";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { useFlowStepUses, useFlowTemplates } from "@/features/workflow/hooks/use-flow-parts";
import { WorkflowTabsComponent } from "@/features/workflow/components/WorkflowTabsComponent";
import { WorkflowContentComponent } from "@/features/workflow/components/WorkflowContentComponent";
import { CopiedStepBarComponent } from "@/features/workflow/components/CopiedStepBarComponent";
import { WorkflowTemplateGridComponent } from "@/features/workflow/components/WorkflowTemplateGridComponent";
import { WorkflowTemplatePanelComponent } from "@/features/workflow/components/WorkflowTemplatePanelComponent";
import { WorkflowScriptComponent } from "@/features/workflow/components/WorkflowScriptComponent";
import type { WorkflowPartSelection } from "@/features/workflow/components/WorkflowPartListComponent";
import { FlowAreaListComponent } from "@/features/flow-area/components/FlowAreaListComponent";
import { FlowAreaPanelComponent } from "@/features/flow-area/components/FlowAreaPanelComponent";
import { FlowPointListComponent } from "@/features/flow-point/components/FlowPointListComponent";
import { FlowPointPanelComponent } from "@/features/flow-point/components/FlowPointPanelComponent";
import { FlowCsvColumnListComponent } from "@/features/flow-csv-column/components/FlowCsvColumnListComponent";
import { FlowCsvColumnPanelComponent } from "@/features/flow-csv-column/components/FlowCsvColumnPanelComponent";

interface Props {
  flowId: number;
  flow: FlowDto | undefined;
}

/**
 * The editor below its header: the tabs and what each lists on the left, the picked one's form on
 * the right. The Script tab takes the whole width.
 *
 * The Flow tab is hidden rather than unmounted while another is open, so the tree keeps the
 * branches that were open and a half-edited step keeps its edits.
 */
export function WorkflowEditorComponent({ flowId, flow }: Props) {
  const activeTab = useWorkflowStore((x) => x.activeTab);
  const treeZoom = useWorkflowStore((x) => x.treeZoom);
  const { data: uses } = useFlowStepUses(flowId);
  const { data: templates } = useFlowTemplates(flowId);

  const [area, setArea] = useState<WorkflowPartSelection>(null);
  const [location, setLocation] = useState<WorkflowPartSelection>(null);
  const [input, setInput] = useState<WorkflowPartSelection>(null);
  const [templateId, setTemplateId] = useState<number | null>(null);

  const isFlow = activeTab === "flow";
  const isScript = activeTab === "script";

  const tabs = (
    <WorkflowTabsComponent
      areaCount={flow?.flowAreas.length}
      locationCount={flow?.flowPoints.length}
      inputCount={flow?.flowCsvColumns.length}
      templateCount={templates?.length}
    />
  );

  return (
    <div className="flex-1 min-h-0">
      <Splitter
        stateKey="workflow-editor-splitter" // remembers the user's choice in localStorage
        stateStorage="local"
        layout="horizontal"
        gutterSize={6}
        className={classNames("h-full border-none border-noround", { hidden: isScript })}
      >
        <SplitterPanel
          size={58}
          minSize={25}
          className="flex flex-column min-w-0"
          style={{ overflow: "hidden" }}
        >
          {tabs}

          {/* Never flex and hidden together: PrimeFlex's flex comes later and wins. */}
          <div className={classNames("flex-column flex-1 min-h-0", isFlow ? "flex" : "hidden")}>
            <CopiedStepBarComponent />
            <ScrollPanel
              className="flex-1 px-2"
              style={{ minHeight: 0 }}
            >
              {/* zoom rather than a font size: the rows' padding, icons and thumbnails are in rem
                  and would not follow. */}
              <div style={{ zoom: treeZoom / 100 }}>
                <DataTreeComponent flowId={flowId} />
              </div>
            </ScrollPanel>
          </div>

          {!isFlow && flow && (
            <ScrollPanel
              className="flex-1"
              style={{ minHeight: 0 }}
            >
              {activeTab === "areas" && (
                <FlowAreaListComponent
                  areas={flow.flowAreas}
                  uses={uses}
                  selected={area}
                  onSelect={setArea}
                />
              )}
              {activeTab === "locations" && (
                <FlowPointListComponent
                  points={flow.flowPoints}
                  areas={flow.flowAreas}
                  uses={uses}
                  selected={location}
                  onSelect={setLocation}
                />
              )}
              {activeTab === "inputs" && (
                <FlowCsvColumnListComponent
                  inputs={flow.flowCsvColumns}
                  uses={uses}
                  selected={input}
                  onSelect={setInput}
                />
              )}
              {activeTab === "templates" && (
                <WorkflowTemplateGridComponent
                  templates={templates}
                  selected={templateId}
                  onSelect={setTemplateId}
                />
              )}
            </ScrollPanel>
          )}
        </SplitterPanel>

        <SplitterPanel
          size={42}
          minSize={20}
          className="flex flex-column min-w-0"
          style={{ overflow: "hidden" }}
        >
          <ScrollPanel
            className={classNames("flex-1", { hidden: !isFlow })}
            style={{ minHeight: 0 }}
          >
            <div className="p-4">
              <WorkflowContentComponent />
            </div>
          </ScrollPanel>

          {!isFlow && flow && (
            <ScrollPanel
              className="flex-1"
              style={{ minHeight: 0 }}
            >
              <div className="p-4">
                {activeTab === "areas" && (
                  <FlowAreaPanelComponent
                    flow={flow}
                    uses={uses}
                    selected={area}
                    onSelect={setArea}
                  />
                )}
                {activeTab === "locations" && (
                  <FlowPointPanelComponent
                    flow={flow}
                    uses={uses}
                    selected={location}
                    onSelect={setLocation}
                  />
                )}
                {activeTab === "inputs" && (
                  <FlowCsvColumnPanelComponent
                    flow={flow}
                    uses={uses}
                    selected={input}
                    onSelect={setInput}
                  />
                )}
                {activeTab === "templates" && (
                  <WorkflowTemplatePanelComponent
                    template={templates?.find((x) => x.id === templateId)}
                    uses={uses}
                  />
                )}
              </div>
            </ScrollPanel>
          )}
        </SplitterPanel>
      </Splitter>

      {isScript && (
        <div className="flex flex-column h-full">
          {tabs}
          <ScrollPanel
            className="flex-1"
            style={{ minHeight: 0 }}
          >
            <WorkflowScriptComponent flowId={flowId} />
          </ScrollPanel>
        </div>
      )}
    </div>
  );
}
