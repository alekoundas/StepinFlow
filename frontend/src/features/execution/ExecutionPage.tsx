import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Splitter, SplitterPanel } from "primereact/splitter";
import { ScrollPanel } from "primereact/scrollpanel";
import { TabPanel, TabView } from "primereact/tabview";

import LabelComponent from "@/shared/components/LabelComponent";
import ExecutionToolbarComponent from "@/features/execution/components/ExecutionToolbarComponent";
import ExecutionFlowTreeComponent from "@/features/execution/components/ExecutionFlowTreeComponent";
import ExecutionStepListComponent from "@/features/execution/components/ExecutionStepListComponent";
import ExecutionStepDetailComponent from "@/features/execution/components/ExecutionStepDetailComponent";
import ExecutionHistoryComponent from "@/features/execution/components/ExecutionHistoryComponent";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionEvents } from "@/features/execution/hooks/use-execution-events";
import { useExecutionState } from "@/features/execution/hooks/use-execution";
import { useFlow } from "@/features/flow/hooks/use-flow";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";

export default function ExecutionPage() {
  const { id } = useParams<{
    id?: string; // Flow Id
  }>();

  const flowId = id ? +id : -1;

  const {
    executionSteps,
    selectedSequence,
    setExecutionSteps,
    setRunState,
    setExecutionId,
  } = useExecutionStore();

  // The run being looked at. Live while one is going, and whatever was opened from History after.
  const [openedExecution, setOpenedExecution] = useState<ExecutionDto | undefined>(
    undefined,
  );

  const { data: flow } = useFlow(flowId > 0 ? flowId : null);
  const { data: engineState } = useExecutionState();

  useExecutionEvents(flowId);

  // The engine outlives the page, so a run started before this mounted is still going.
  useEffect(() => {
    if (!engineState) return;

    setRunState(engineState.state);

    if (engineState.isRunning) setExecutionId(engineState.executionId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [engineState]);

  const selectedStep = executionSteps.find((x) => x.sequence === selectedSequence);

  const handleOpenExecution = (execution: ExecutionDto) => {
    setOpenedExecution(execution);
    setExecutionSteps(execution.executionSteps ?? []);
  };

  return (
    <div className="flex flex-column m-4 mr-3">
      {/* Title */}
      <div className="flex flex-wrap justify-content-between items-center">
        <LabelComponent
          text={flow?.name ?? "Run"}
          size="5xl"
          weight="bold"
        />
      </div>

      <div
        className="mt-4"
        style={{ height: "78vh" }}
      >
        <TabView className="h-full">
          <TabPanel header="Run">
            <div className="flex flex-column border-1 surface-border border-round overflow-hidden h-full">
              <ExecutionToolbarComponent flowId={flowId} />

              <Splitter
                stateKey="execution-splitter" //  remembers user choice in localStorage
                stateStorage="local"
                layout="horizontal"
                gutterSize={10} // thickness of draggable bar
                className="flex-auto"
              >
                <SplitterPanel
                  size={25} // default size
                  minSize={15} // can't shrink below 15%
                  className="flex flex-column"
                  style={{ overflow: "hidden" }}
                >
                  <ScrollPanel className="h-full">
                    <ExecutionFlowTreeComponent flowId={flowId} />
                  </ScrollPanel>
                </SplitterPanel>

                <SplitterPanel
                  size={50} // the run is what you actually read, so it gets the room
                  minSize={25}
                  className="flex flex-column"
                  style={{ overflow: "hidden" }}
                >
                  <ScrollPanel className="h-full">
                    <ExecutionStepListComponent
                      errorFlowStepId={openedExecution?.errorFlowStepId}
                    />
                  </ScrollPanel>
                </SplitterPanel>

                <SplitterPanel
                  size={25}
                  minSize={15}
                  className="flex flex-column"
                  style={{ overflow: "hidden" }}
                >
                  <ScrollPanel className="h-full">
                    <ExecutionStepDetailComponent
                      executionStep={selectedStep}
                      executionSteps={executionSteps}
                    />
                  </ScrollPanel>
                </SplitterPanel>
              </Splitter>
            </div>
          </TabPanel>

          <TabPanel header="History">
            <ExecutionHistoryComponent
              flowId={flowId}
              onOpen={handleOpenExecution}
            />
          </TabPanel>
        </TabView>
      </div>
    </div>
  );
}
