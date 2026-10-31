import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { Splitter, SplitterPanel } from "primereact/splitter";

import ExecutionHeaderComponent from "@/features/execution/components/ExecutionHeaderComponent";
import ExecutionTilesComponent from "@/features/execution/components/ExecutionTilesComponent";
import ExecutionActivityComponent from "@/features/execution/components/ExecutionActivityComponent";
import ExecutionFlowTreeComponent from "@/features/execution/components/ExecutionFlowTreeComponent";
import ExecutionStepListComponent from "@/features/execution/components/ExecutionStepListComponent";
import ExecutionStepDetailComponent from "@/features/execution/components/ExecutionStepDetailComponent";
import ExecutionPanelComponent from "@/features/execution/components/templates/ExecutionPanelComponent";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionEvents } from "@/features/execution/hooks/use-execution-events";
import { useExecution, useExecutionState } from "@/features/execution/hooks/use-execution";
import { findStep, useExecutionOverview } from "@/features/execution/hooks/use-execution-overview";
import { MUTED } from "@/features/execution/execution-format";
import { useFlow } from "@/features/flow/hooks/use-flow";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";

export default function ExecutionPage() {
  const { id } = useParams<{
    id?: string; // Flow Id
  }>();

  const [searchParams] = useSearchParams();
  const requestedExecutionId = searchParams.get("executionId");

  const flowId = id ? +id : -1;

  const executionSteps = useExecutionStore((x) => x.executionSteps);
  const selectedSequence = useExecutionStore((x) => x.selectedSequence);
  const runState = useExecutionStore((x) => x.runState);
  const liveExecutionId = useExecutionStore((x) => x.executionId);
  const liveHistoryLevel = useExecutionStore((x) => x.historyLevel);
  const setSelectedSequence = useExecutionStore((x) => x.setSelectedSequence);
  const setExecutionSteps = useExecutionStore((x) => x.setExecutionSteps);
  const setRunState = useExecutionStore((x) => x.setRunState);
  const setExecutionId = useExecutionStore((x) => x.setExecutionId);

  // The execution being read back, when one was opened from the executions list.
  const [openedExecution, setOpenedExecution] = useState<ExecutionDto | undefined>(
    undefined,
  );

  // Arrived from the executions list with an execution named. Open that one rather than the last state.
  const {
    data: requestedExecution,
    isLoading: isRequestedExecutionLoading,
    isError: isRequestedExecutionError,
    error: requestedExecutionError,
  } = useExecution(requestedExecutionId ? +requestedExecutionId : null);

  const { data: flow } = useFlow(flowId > 0 ? flowId : null);
  const { data: engineState } = useExecutionState();

  useExecutionEvents(flowId);

  // The engine outlives the page, so an execution started before this mounted is still going.
  useEffect(() => {
    if (!engineState) return;

    setRunState(engineState.state);

    if (engineState.isRunning) setExecutionId(engineState.executionId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [engineState]);

  useEffect(() => {
    if (!requestedExecution || openedExecution?.id === requestedExecution.id) return;

    setOpenedExecution(requestedExecution);
    setExecutionSteps(requestedExecution.executionSteps ?? []);
  }, [requestedExecution, openedExecution, setExecutionSteps]);

  // Arrived from the executions list with an execution named, so this is history being read back.
  const isPastExecution = requestedExecutionId !== null;
  const isLive = !isPastExecution && runState !== RunStateEnum.FINISHED;

  const overview = useExecutionOverview(
    executionSteps,
    openedExecution?.errorFlowStepId,
    isLive,
  );

  const selectedStep =
    selectedSequence !== undefined ? findStep(executionSteps, selectedSequence) : undefined;

  // Only a past execution can fail to load: a live one builds its rows from the events as they arrive.
  let emptyReason: "idle" | "loading" | "error" | "noHistory" = "idle";
  if (isPastExecution && isRequestedExecutionLoading) emptyReason = "loading";
  else if (isPastExecution && isRequestedExecutionError) emptyReason = "error";
  else if (isPastExecution) emptyReason = "noHistory";

  return (
    <div
      className="flex flex-column gap-3 p-4 h-full"
      style={{ minHeight: 760 }}
    >
      <ExecutionHeaderComponent
        flowId={flowId}
        flowName={flow?.name ?? "Execution"}
        execution={openedExecution}
        isPastExecution={isPastExecution}
      />

      <ExecutionTilesComponent
        overview={overview}
        stepCount={executionSteps.length}
        isLive={isLive}
      />

      <ExecutionActivityComponent
        activity={overview.activity}
        isLive={isLive}
      />

      <Splitter
        stateKey="execution-splitter" //  remembers user choice in localStorage
        stateStorage="local"
        layout="horizontal"
        gutterSize={10} // thickness of draggable bar
        className="flex-1 border-none"
        style={{ background: "transparent", minHeight: 0 }}
      >
        <SplitterPanel
          size={24}
          minSize={15}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <ExecutionPanelComponent
            title="Flow"
            actions={
              <span
                className="text-xs"
                style={MUTED}
              >
                times executed
              </span>
            }
          >
            <ExecutionFlowTreeComponent
              flowId={flowId}
              countsByFlowStepId={overview.countsByFlowStepId}
            />
          </ExecutionPanelComponent>
        </SplitterPanel>

        <SplitterPanel
          size={50} // the execution is what you actually read, so it gets the room
          minSize={25}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          {/* Keyed, so a new execution starts with no folds open, no filter and at the top. */}
          <ExecutionStepListComponent
            key={(isPastExecution ? openedExecution?.id : liveExecutionId) ?? "none"}
            overview={overview}
            isLive={isLive}
            emptyReason={emptyReason}
            emptyMessage={
              isRequestedExecutionError
                ? (requestedExecutionError as Error)?.message
                : undefined
            }
          />
        </SplitterPanel>

        <SplitterPanel
          size={26}
          minSize={15}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <ExecutionStepDetailComponent
            executionStep={selectedStep}
            executionSteps={executionSteps}
            fatalSequence={overview.fatalSequence}
            historyLevel={isPastExecution ? openedExecution?.historyLevel : liveHistoryLevel}
            onSelect={setSelectedSequence}
          />
        </SplitterPanel>
      </Splitter>
    </div>
  );
}
