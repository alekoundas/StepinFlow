import { useState } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";
import type { RepositoryStatusDto } from "@/shared/models/repository-dtos";
import { FlowDataTableComponent } from "@/features/flow/components/FlowDataTableComponent";
import FlowSearchComponent from "@/features/flow/components/FlowSearchComponent";
import BranchMenuComponent from "@/features/repository/components/BranchMenuComponent";
import { useFlows } from "@/features/flow/hooks/use-flows";
import { useFlowHealth } from "@/features/flow/hooks/use-flow-health";
import { useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import { useRecordingStore } from "@/features/recording/store/recording-store";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FlowCreateChoiceDialogComponent } from "@/features/flow/components/FlowCreateChoiceDialogComponent";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

type FilterId = "all" | "automation" | "qa" | "issues" | "uncommitted";

interface Filter {
  id: FilterId;
  label: string;
}

const FLOW_FILTERS: Filter[] = [
  { id: "all", label: "All" },
  { id: "automation", label: "Automation" },
  { id: "qa", label: "QA tests" },
  { id: "issues", label: "With issues" },
  { id: "uncommitted", label: "Not committed" },
];

// A sub-flow is called, not started, so whether it is a test or an automation is its caller's.
const SUB_FLOW_FILTERS: Filter[] = FLOW_FILTERS.filter(
  (x) => x.id !== "automation" && x.id !== "qa",
);

interface Props {
  /** Sub-flows are the same page with the flag flipped: same table, same recorder, same editor. */
  isSubFlow?: boolean;
}

export default function FlowListPage({ isSubFlow = false }: Props) {
  const navigate = useNavigate();
  const { setTarget, setCreateAsSubFlow } = useRecordingStore();
  const { openCustom } = useDialogStore();
  const [picked, setPicked] = useState<FilterId>("all");

  const { data, isLoading } = useFlows({
    first: 0,
    rows: 0,
    page: 0,
    isSubFlow,
  });
  const health = useFlowHealth();
  const { data: status } = useRepositoryStatus();

  const noun = isSubFlow ? "Sub-Flow" : "Flow";
  const filters = isSubFlow ? SUB_FLOW_FILTERS : FLOW_FILTERS;

  // The page stays mounted between Flows and Sub-flows, so a filter only one of them has falls back.
  const filter = filters.some((x) => x.id === picked) ? picked : "all";
  const all: FlowDto[] = data?.data ?? [];
  const flows = all.filter((x) => isShown(x, filter, health, status));

  let emptyMessage = "No flow matches this filter.";
  if (all.length === 0)
    emptyMessage = isSubFlow
      ? "No sub-flows yet."
      : "No flows yet. New flow records one, or Import brings one in.";

  const handleNew = () => navigate("/flows/new");

  // Recording was a button of its own next to New, which read as a different kind of thing rather
  // than another way of doing the same thing. All three start here now.
  const handleCreate = () => {
    openCustom(
      "flow-create-choice",
      <FlowCreateChoiceDialogComponent
        noun={noun}
        onManual={isSubFlow ? undefined : handleNew}
        onRecord={handleRecord}
        onAi={handleCreateWithAi}
      />,
    );
  };

  const handleCreateWithAi = () => {
    setTarget(undefined);
    setCreateAsSubFlow(isSubFlow);
    navigate("/flows/ai");
  };

  // The recorder names and creates the flow itself, so its step forms have somewhere to list
  // search areas and points from.
  const handleRecord = () => {
    setTarget(undefined);
    setCreateAsSubFlow(isSubFlow);
    navigate("/record");
  };

  return (
    <>
      <header className="flex align-items-center justify-content-between flex-wrap gap-3 px-4 py-3 border-bottom-1 surface-border">
        <div className="flex align-items-center gap-3">
          <span
            className="text-sm"
            style={MUTED}
          >
            {isSubFlow ? "Sub-flows" : "Flows"}
          </span>
          {status && (
            <div className="border-1 surface-border border-round-md">
              <BranchMenuComponent
                flowId={undefined}
                branch={status.branch}
                commitCount={status.commitCount}
                changeCount={status.changeCount}
              />
            </div>
          )}
        </div>

        <FlowSearchComponent />
      </header>

      <div
        className="m-4 flex flex-column gap-4"
        style={{ maxWidth: 1240 }}
      >
        <div className="flex align-items-end justify-content-between flex-wrap gap-3">
          <div className="flex flex-column gap-1">
            <h1
              className="m-0 text-3xl font-semibold"
              style={{ letterSpacing: "-0.02em" }}
            >
              {isSubFlow ? "Sub-flows" : "Flows"}
            </h1>
            <p
              className="m-0"
              style={MUTED}
            >
              {isSubFlow
                ? "Flows that another flow calls, rather than ones started on their own."
                : "Everything you can execute. Sub-flows have their own list."}
            </p>
          </div>

          <Button
            label={isSubFlow ? "New sub-flow" : "New flow"}
            icon="pi pi-plus"
            onClick={handleCreate}
          />
        </div>

        <div
          role="group"
          aria-label="Filter"
          className="flex flex-wrap gap-2"
        >
          {filters.map((x) => (
            <Button
              key={x.id}
              type="button"
              label={x.label}
              size="small"
              rounded
              outlined
              severity={x.id === filter ? undefined : "secondary"}
              aria-pressed={x.id === filter}
              onClick={() => setPicked(x.id)}
            />
          ))}
        </div>

        <FlowDataTableComponent
          flows={flows}
          isLoading={isLoading}
          isSubFlow={isSubFlow}
          emptyMessage={emptyMessage}
        />
      </div>
    </>
  );
}

const isShown = (
  flow: FlowDto,
  filter: FilterId,
  health: Map<number, FlowHealthDto>,
  status: RepositoryStatusDto | undefined,
): boolean => {
  const counts = health.get(flow.id);

  switch (filter) {
    case "automation":
      return !flow.isQaTest;
    case "qa":
      return flow.isQaTest;
    case "issues":
      return (counts?.errorCount ?? 0) + (counts?.warningCount ?? 0) > 0;
    case "uncommitted":
      return (
        status?.flows.some((x) => x.flowId === flow.id && x.changeCount > 0) ??
        false
      );
    default:
      return true;
  }
};
