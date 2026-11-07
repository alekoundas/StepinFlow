import { useNavigate, useSearchParams } from "react-router-dom";

import PageComponent from "@/shared/components/PageComponent";
import type { ExecutionFeedItemDto } from "@/shared/models/database/execution-feed-item-dto";
import { MUTED } from "@/features/execution/execution-format";
import { useExecutionStats, useFlowExecutionSummaries } from "@/features/execution/hooks/use-execution";
import { useLiveExecution } from "@/features/execution/hooks/use-live-execution";
import ExecutionFeedComponent from "@/features/execution/components/executions/ExecutionFeedComponent";
import ExecutionFlowListComponent from "@/features/execution/components/executions/ExecutionFlowListComponent";
import ExecutionStatTilesComponent from "@/features/execution/components/executions/ExecutionStatTilesComponent";
import LiveExecutionBannerComponent from "@/features/execution/components/executions/LiveExecutionBannerComponent";
import PickedFlowHeaderComponent from "@/features/execution/components/executions/PickedFlowHeaderComponent";

/**
 * Everything this machine has executed, newest first, with what is executing now above it. It first
 * answers "what happened lately?" for every flow; a flow picked on the left narrows it to that one.
 * The flow picked is in the address, so coming back from an execution finds it still picked.
 */
export default function ExecutionsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const { data: summaries } = useFlowExecutionSummaries();
  const { data: allStats } = useExecutionStats(null, null);
  const live = useLiveExecution();

  const selectedFlowId = Number(searchParams.get("flow")) || null;
  const picked = summaries?.find((x) => x.flowId === selectedFlowId);

  const select = (flowId: number | null) => {
    setSearchParams(flowId === null ? {} : { flow: String(flowId) }, { replace: true });
  };

  // The live one opens where it is going; one that ended opens as it was left.
  const open = (execution: ExecutionFeedItemDto) => {
    if (execution.id === live?.executionId) {
      navigate(`/execution/${execution.flowId}`);
      return;
    }

    navigate(`/execution/${execution.flowId}?executionId=${execution.id}`);
  };

  let liveFlowName = "A flow";
  if (live?.execution) liveFlowName = live.execution.flowName;
  const liveSummary = summaries?.find((x) => x.flowId === live?.flowId);
  if (liveSummary) liveFlowName = liveSummary.flowName;

  return (
    <PageComponent
      width="wide"
      className="flex flex-column gap-4"
    >
      <div className="flex flex-column gap-1">
        <h1
          className="m-0 text-3xl font-semibold"
          style={{ letterSpacing: "-0.02em" }}
        >
          Executions
        </h1>
        <p
          className="m-0"
          style={MUTED}
        >
          Everything this machine has executed, newest first.
        </p>
      </div>

      {live && (
        <LiveExecutionBannerComponent
          flowName={liveFlowName}
          executionId={live.executionId}
          stepCount={live.stepCount}
          execution={live.execution}
          onView={() => navigate(`/execution/${live.flowId}`)}
        />
      )}

      <div className="flex align-items-start flex-wrap gap-4">
        <div style={{ flex: "0 0 260px", maxWidth: "100%", alignSelf: "stretch" }}>
          <ExecutionFlowListComponent
            summaries={summaries}
            allCount={allStats?.total}
            selectedFlowId={selectedFlowId}
            executingFlowId={live?.flowId ?? null}
            onSelect={select}
          />
        </div>

        <div
          className="flex flex-column gap-3 min-w-0"
          style={{ flex: "1 1 560px" }}
        >
          {picked && <PickedFlowHeaderComponent summary={picked} />}
          <ExecutionStatTilesComponent flowId={selectedFlowId} />
          <ExecutionFeedComponent
            flowId={selectedFlowId}
            live={live}
            showsFlowName={selectedFlowId === null}
            onOpen={open}
          />
        </div>
      </div>
    </PageComponent>
  );
}
