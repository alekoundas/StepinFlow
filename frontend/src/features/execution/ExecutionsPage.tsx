import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Splitter, SplitterPanel } from "primereact/splitter";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import PanelHeaderComponent from "@/shared/components/PanelHeaderComponent";
import ExecutionHistoryComponent from "@/features/execution/components/ExecutionHistoryComponent";
import { useFlowExecutionSummaries } from "@/features/execution/hooks/use-execution";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";

/**
 * Every flow and how its runs have been going.
 *
 * The run page needs a flow id, so until now there was no way into it that did not start from a
 * flow. This is that way in: pick a flow on the left, read its runs on the right, open one.
 */
export default function ExecutionsPage() {
  const navigate = useNavigate();

  const { data: summaries, isLoading } = useFlowExecutionSummaries();
  const [selectedFlowId, setSelectedFlowId] = useState<number | null>(null);

  // Land on something worth reading: the flow that ran most recently, not the first alphabetically.
  useEffect(() => {
    if (selectedFlowId !== null || !summaries?.length) return;

    const ran = summaries
      .filter((x) => x.lastRunOn)
      .sort((a, b) => (a.lastRunOn! < b.lastRunOn! ? 1 : -1));

    setSelectedFlowId(ran[0]?.flowId ?? summaries[0].flowId);
  }, [summaries, selectedFlowId]);

  const selected = summaries?.find((x) => x.flowId === selectedFlowId);

  return (
    <div className="flex flex-column gap-3 p-4 h-full">
      <div className="flex flex-column gap-1">
        <LabelComponent
          text="Executions"
          size="2xl"
          weight="bold"
        />
        <LabelComponent
          text="Every run this machine has recorded."
          size="sm"
          color="secondary"
        />
      </div>

      <Splitter
        className="flex-1 border-none"
        stateKey="executions-splitter"
        stateStorage="local"
        gutterSize={10}
        style={{ background: "transparent", minHeight: 0 }}
      >
        <SplitterPanel
          size={32}
          minSize={20}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <PanelCard title="Flows">
            <div className="flex flex-column gap-2 p-2">
              {isLoading ? (
                <LabelComponent
                  text="Loading."
                  size="sm"
                  color="secondary"
                />
              ) : null}

              {summaries?.map((summary) => (
                <FlowSummaryCard
                  key={summary.flowId}
                  summary={summary}
                  isSelected={summary.flowId === selectedFlowId}
                  onSelect={() => setSelectedFlowId(summary.flowId)}
                />
              ))}
            </div>
          </PanelCard>
        </SplitterPanel>

        <SplitterPanel
          size={68}
          minSize={40}
          className="flex flex-column"
          style={{ overflow: "hidden" }}
        >
          <PanelCard
            title={selected ? `Runs of ${selected.flowName}` : "Runs"}
            actions={
              selectedFlowId ? (
                <Button
                  label="Run flow"
                  icon="pi pi-play"
                  size="small"
                  onClick={() => navigate(`/execution/${selectedFlowId}`)}
                />
              ) : null
            }
          >
            {selectedFlowId ? (
              <ExecutionHistoryComponent
                flowId={selectedFlowId}
                onOpen={(execution) =>
                  navigate(`/execution/${selectedFlowId}?executionId=${execution.id}`)
                }
              />
            ) : (
              <div className="p-3">
                <LabelComponent
                  text="Pick a flow to see its runs."
                  size="sm"
                  color="secondary"
                />
              </div>
            )}
          </PanelCard>
        </SplitterPanel>
      </Splitter>
    </div>
  );
}

interface PanelCardProps {
  title: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
}

/** Its own surface, its own header, its own scroll - the same panel as the run page. */
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

interface FlowSummaryCardProps {
  summary: FlowExecutionSummaryDto;
  isSelected: boolean;
  onSelect: () => void;
}

function FlowSummaryCard({ summary, isSelected, onSelect }: FlowSummaryCardProps) {
  const hasRun = summary.runCount > 0;
  const successRate = hasRun
    ? Math.round((summary.completedCount / summary.runCount) * 100)
    : 0;

  return (
    <div
      onClick={onSelect}
      className="border-1 border-round p-3 flex flex-column gap-2 cursor-pointer"
      style={{
        borderColor: isSelected ? "var(--primary-color)" : "var(--surface-border)",
        background: isSelected ? "var(--highlight-bg)" : "var(--surface-ground)",
      }}
    >
      <div className="flex align-items-center gap-2">
        <LabelComponent
          text={summary.flowName}
          weight="semibold"
          color={isSelected ? "primary" : undefined}
        />

        {summary.isSubFlow ? (
          <LabelComponent
            text="sub-flow"
            size="xs"
            color="secondary"
          />
        ) : null}
      </div>

      {hasRun ? (
        <div className="flex align-items-end justify-content-between gap-3">
          <OutcomeSparkline outcomes={summary.recentOutcomes} />

          <div className="flex align-items-baseline gap-2">
            <LabelComponent
              text={`${successRate}%`}
              weight="semibold"
            />
            <LabelComponent
              text={`of ${summary.runCount} runs`}
              size="xs"
              color="secondary"
            />
          </div>
        </div>
      ) : (
        <LabelComponent
          text="Never run"
          size="xs"
          color="secondary"
        />
      )}
    </div>
  );
}

interface OutcomeSparklineProps {
  outcomes: ExecutionStatusEnum[];
}

/**
 * The last few runs as a row of bars, oldest first. A flow that has started failing looks different
 * from one that always has, which a success percentage on its own cannot say.
 */
function OutcomeSparkline({ outcomes }: OutcomeSparklineProps) {
  return (
    <div
      className="flex align-items-end gap-1"
      style={{ height: 22 }}
    >
      {outcomes.map((outcome, index) => (
        <div
          key={index}
          title={outcome}
          style={{
            width: 4,
            height: outcome === ExecutionStatusEnum.COMPLETED ? "100%" : "55%",
            borderRadius: 1,
            background: outcomeColour(outcome),
          }}
        />
      ))}
    </div>
  );
}

function outcomeColour(outcome: ExecutionStatusEnum): string {
  switch (outcome) {
    case ExecutionStatusEnum.COMPLETED:
      return "var(--green-400)";
    case ExecutionStatusEnum.ERRORED:
      return "var(--red-400)";
    case ExecutionStatusEnum.RUNNING:
      return "var(--yellow-500)";
    default:
      return "var(--orange-400)";
  }
}
