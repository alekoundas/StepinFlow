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
import { findKeptStep, useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionEvents } from "@/features/execution/hooks/use-execution-events";
import { useExecution, useExecutionState } from "@/features/execution/hooks/use-execution";
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

  const overview = useExecutionStore((x) => x.overview);
  const selectedStep = useExecutionStore((x) =>
    x.selectedSequence !== undefined ? findKeptStep(x, x.selectedSequence) : undefined,
  );
  const parentStep = useExecutionStore((x) =>
    selectedStep?.parentSequence !== null && selectedStep?.parentSequence !== undefined
      ? findKeptStep(x, selectedStep.parentSequence)
      : undefined,
  );
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

  // Taken during render rather than in an effect, so the header never paints one frame with the
  // execution opened before it. A refetch of the same one leaves its rows as they are.
  if (requestedExecution && openedExecution?.id !== requestedExecution.id)
    setOpenedExecution(requestedExecution);

  useEffect(() => {
    if (openedExecution)
      setExecutionSteps(openedExecution.executionSteps ?? [], openedExecution.errorFlowStepId);
  }, [openedExecution, setExecutionSteps]);

  // Arrived from the executions list with an execution named, so this is history being read back.
  const isPastExecution = requestedExecutionId !== null;
  const isLive = !isPastExecution && runState !== RunStateEnum.FINISHED;

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
            parentStep={parentStep}
            fatalSequence={overview.fatalSequence}
            historyLevel={isPastExecution ? openedExecution?.historyLevel : liveHistoryLevel}
            executionStatus={isPastExecution ? openedExecution?.status : undefined}
            isLive={isLive}
            onSelect={setSelectedSequence}
          />
        </SplitterPanel>
      </Splitter>
    </div>
  );
}
