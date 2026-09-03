import { useEffect, useState, type ReactNode } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { Splitter, SplitterPanel } from "primereact/splitter";

import ExecutionToolbarComponent from "@/features/execution/components/ExecutionToolbarComponent";
import ExecutionFlowTreeComponent from "@/features/execution/components/ExecutionFlowTreeComponent";
import ExecutionStepListComponent from "@/features/execution/components/ExecutionStepListComponent";
import ExecutionStepDetailComponent from "@/features/execution/components/ExecutionStepDetailComponent";
import RunHeaderComponent from "@/features/execution/components/RunHeaderComponent";
import PanelHeaderComponent from "@/shared/components/PanelHeaderComponent";
import { Button } from "primereact/button";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionEvents } from "@/features/execution/hooks/use-execution-events";
import { useExecution, useExecutionState } from "@/features/execution/hooks/use-execution";
import { useFlow } from "@/features/flow/hooks/use-flow";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";

export default function ExecutionPage() {
  const { id } = useParams<{
    id?: string; // Flow Id
  }>();

  const [searchParams] = useSearchParams();
  const requestedExecutionId = searchParams.get("executionId");

  const flowId = id ? +id : -1;

  const {
    executionSteps,
    selectedSequence,
    setSelectedSequence,
    setExecutionSteps,
    setRunState,
    setExecutionId,
  } = useExecutionStore();

  // The run being looked at. Live while one is going, and whatever was opened from History after.
  const [openedExecution, setOpenedExecution] = useState<ExecutionDto | undefined>(
    undefined,
  );

  // Arrived from the executions list with a run named. Open that one rather than the last state.
  const { data: requestedExecution } = useExecution(
    requestedExecutionId ? +requestedExecutionId : null,
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

  const [showFailuresOnly, setShowFailuresOnly] = useState(false);

  // Arrived from the executions list with a run named, so this is history being read back.
  const isPastRun = requestedExecutionId !== null;

  // The one failure that ended the run, which is what "jump to failure" means.
  const fatalSequence = openedExecution?.errorFlowStepId
    ? executionSteps.find((x) => x.flowStepId === openedExecution.errorFlowStepId)
        ?.sequence
    : undefined;

  useEffect(() => {
    if (!requestedExecution || openedExecution?.id === requestedExecution.id) return;

    setOpenedExecution(requestedExecution);
    setExecutionSteps(requestedExecution.executionSteps ?? []);
  }, [requestedExecution, openedExecution, setExecutionSteps]);


  return (
    <div className="flex flex-column gap-3 p-4 h-full">
      <RunHeaderComponent
        flowName={flow?.name ?? "Run"}
        execution={openedExecution}
        executionSteps={executionSteps}
        isPastRun={isPastRun}
      />

      {/* A past run has already happened. Offering to start, pause or step it is a lie. */}
      {isPastRun ? null : <ExecutionToolbarComponent flowId={flowId} />}

      <Splitter
        stateKey="execution-splitter" //  remembers user choice in localStorage
        stateStorage="local"
        layout="horizontal"
        gutterSize={10} // thickness of draggable bar
        className="flex-1 border-none"
        style={{ background: "transparent", minHeight: 0 }}
      >
        <SplitterPanel
          size={25} // default size
          minSize={15} // can't shrink below 15%
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <PanelCard title="Flow">
            <ExecutionFlowTreeComponent flowId={flowId} />
          </PanelCard>
        </SplitterPanel>

        <SplitterPanel
          size={50} // the run is what you actually read, so it gets the room
          minSize={25}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <PanelCard
            title="Run"
            actions={
              <>
                <Button
                  label="Failures only"
                  icon="pi pi-filter"
                  size="small"
                  text={!showFailuresOnly}
                  outlined={showFailuresOnly}
                  onClick={() => setShowFailuresOnly(!showFailuresOnly)}
                />
                <Button
                  label="Jump to failure"
                  icon="pi pi-arrow-down"
                  size="small"
                  text
                  disabled={fatalSequence === undefined}
                  onClick={() =>
                    fatalSequence !== undefined && setSelectedSequence(fatalSequence)
                  }
                />
              </>
            }
          >
            <ExecutionStepListComponent
              errorFlowStepId={openedExecution?.errorFlowStepId}
              showFailuresOnly={showFailuresOnly}
            />
          </PanelCard>
        </SplitterPanel>

        <SplitterPanel
          size={25}
          minSize={15}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <PanelCard title={selectedStep ? `Step ${selectedStep.sequence}` : "Step"}>
            <ExecutionStepDetailComponent
              executionStep={selectedStep}
              executionSteps={executionSteps}
            />
          </PanelCard>
        </SplitterPanel>
      </Splitter>
    </div>
  );
}

interface PanelCardProps {
  title: string;
  actions?: ReactNode;
  children: ReactNode;
}

/** One of the three panels: its own surface, its own header, its own scroll. */
function PanelCard({ title, actions, children }: PanelCardProps) {
  return (
    <div
      className="flex flex-column surface-card border-1 surface-border border-round h-full"
      style={{ overflow: "hidden", minHeight: 0 }}
    >
      <PanelHeaderComponent title={title}>{actions}</PanelHeaderComponent>

      <div
        className="flex-1"
        style={{ overflow: "auto", minHeight: 0 }}
      >
        {children}
      </div>
    </div>
  );
}
