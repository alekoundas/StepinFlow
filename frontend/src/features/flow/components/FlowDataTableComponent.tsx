import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { Column } from "primereact/column";
import { DataTable } from "primereact/datatable";

import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import StatusPillComponent from "@/shared/components/StatusPillComponent";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import {
  executionStatusSeverity,
  executionStatusText,
} from "@/shared/utils/execution-status";
import FlowHealthBadgeComponent from "@/features/flow/components/FlowHealthBadgeComponent";
import { useDeleteFlow } from "@/features/flow/hooks/use-delete-flow";
import { useFlowHealth } from "@/features/flow/hooks/use-flow-health";
import { useFlowExecutionSummaries } from "@/features/execution/hooks/use-execution";
import UncommittedBadgeComponent from "@/features/repository/components/UncommittedBadgeComponent";
import { MONO } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

type Props = {
  /** Already filtered: the table shows what it is given. */
  flows: FlowDto[];
  isLoading: boolean;

  /** Sub-flows have no kind to tell apart, and a count of the flows that call them instead. */
  isSubFlow: boolean;
  emptyMessage: string;
};

/** The flows list as the mockup's first board draws it: one row a flow, opened by clicking it. */
export function FlowDataTableComponent({
  flows,
  isLoading,
  isSubFlow,
  emptyMessage,
}: Props) {
  const navigate = useNavigate();
  const deleteFlow = useDeleteFlow();
  const health = useFlowHealth();
  const { data: summaries } = useFlowExecutionSummaries();

  // Undefined until they arrive, so a row does not say "Never executed" for a moment first.
  const executions = summaries
    ? new Map(summaries.map((x) => [x.flowId, x]))
    : undefined;

  return (
    <div className="border-1 surface-border border-round-xl overflow-auto">
      <DataTable
        value={flows}
        dataKey="id"
        loading={isLoading}
        emptyMessage={emptyMessage}
        onRowClick={(e) => navigate(`/workflow/${(e.data as FlowDto).id}`)}
        rowClassName={() => "cursor-pointer"}
        tableStyle={{ minWidth: 860 }}
      >
        <Column
          header={isSubFlow ? "Sub-flow" : "Flow"}
          body={(row: FlowDto) => <NameComponent flow={row} />}
        />
        {isSubFlow ? (
          <Column
            header="Used by"
            body={(row: FlowDto) => plural(row.callerCount, "flow")}
          />
        ) : (
          <Column
            header="Kind"
            body={(row: FlowDto) => <KindComponent isQaTest={row.isQaTest} />}
          />
        )}
        <Column
          header="Health"
          body={(row: FlowDto) => (
            <FlowHealthBadgeComponent
              health={health.get(row.id)}
              isEmpty={row.stepCount === 0}
            />
          )}
        />
        <Column
          header="Last execution"
          body={(row: FlowDto) =>
            executions && (
              <LastExecutionComponent
                flow={row}
                summary={executions.get(row.id)}
              />
            )
          }
        />
        <Column
          header="Steps"
          align="right"
          body={(row: FlowDto) => (
            <span style={{ ...MUTED, fontFamily: MONO }}>{row.stepCount}</span>
          )}
        />
        <Column
          align="right"
          body={(row: FlowDto) => (
            // The menu's items are in a portal, and React still bubbles their clicks up to the row.
            <div
              className="flex align-items-center justify-content-end gap-1"
              onClick={(e) => e.stopPropagation()}
            >
              <Button
                type="button"
                label="Execute"
                icon="pi pi-play"
                size="small"
                severity="secondary"
                outlined
                aria-label={`Execute ${row.name}`}
                onClick={() => navigate(`/execution/${row.id}`)}
              />
              <ActionsMenuComponent
                id={row.id}
                onClone={(id) => navigate(`/flows/${id}/clone`)}
                onDelete={() => deleteFlow(row)}
                extraActions={[
                  {
                    label: "Settings",
                    icon: "pi pi-cog",
                    command: (id) => navigate(`/flows/${id}/edit`),
                  },
                ]}
              />
            </div>
          )}
        />
      </DataTable>
    </div>
  );
}

function NameComponent({ flow }: { flow: FlowDto }) {
  return (
    <div className="flex flex-column gap-1">
      <div className="flex align-items-center flex-wrap gap-2">
        <span className="font-medium">{flow.name}</span>
        <UncommittedBadgeComponent flowId={flow.id} />
      </div>
      {flow.description && (
        <span
          className="text-sm"
          style={{ ...MUTED, maxWidth: 420 }}
        >
          {flow.description.split("\n")[0]}
        </span>
      )}
    </div>
  );
}

function KindComponent({ isQaTest }: { isQaTest: boolean }) {
  let style: CSSProperties = { ...MUTED, borderColor: "var(--surface-border)" };
  if (isQaTest)
    style = {
      color: "var(--primary-color)",
      borderColor: "color-mix(in srgb, var(--primary-color) 50%, transparent)",
    };

  return (
    <span
      className="inline-flex align-items-center text-xs border-1 border-round px-2 py-1 white-space-nowrap"
      style={style}
    >
      {isQaTest ? "QA test" : "Automation"}
    </span>
  );
}

interface LastExecutionProps {
  flow: FlowDto;
  summary: FlowExecutionSummaryDto | undefined;
}

// "Completed" over "Today, 18:04"; "Never executed" over when it was last edited.
function LastExecutionComponent({ flow, summary }: LastExecutionProps) {
  let pill = <StatusPillComponent text="Never executed" />;
  let when = `Edited ${editedWhen(flow.updatedOn ?? flow.createdOn)}`;

  if (summary?.lastStatus && summary.lastRunOn) {
    pill = (
      <StatusPillComponent
        text={executionStatusText(summary.lastStatus)}
        severity={executionStatusSeverity(summary.lastStatus)}
        pulse={summary.lastStatus === ExecutionStatusEnum.RUNNING}
      />
    );
    when = dayAndTime(summary.lastRunOn);
  }

  return (
    <div className="flex flex-column align-items-start gap-1">
      {pill}
      <span
        className="text-xs"
        style={MUTED}
      >
        {when}
      </span>
    </div>
  );
}

const plural = (count: number, noun: string) =>
  `${count} ${noun}${count === 1 ? "" : "s"}`;

// Calendar days, so ten past midnight is "Yesterday" for something done at eleven.
const daysAgo = (date: Date): number => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const day = new Date(date);
  day.setHours(0, 0, 0, 0);

  return Math.round((today.getTime() - day.getTime()) / 86_400_000);
};

const shortDate = (date: Date): string =>
  date.toLocaleDateString(undefined, { day: "numeric", month: "short" });

// "Today, 18:04", "Yesterday, 23:10", "20 Oct, 16:03".
const dayAndTime = (value: string): string => {
  const date = new Date(value);
  const time = date.toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
  });
  const days = daysAgo(date);

  if (days === 0) return `Today, ${time}`;
  if (days === 1) return `Yesterday, ${time}`;

  return `${shortDate(date)}, ${time}`;
};

// "today", "3 days ago", "12 Oct".
const editedWhen = (value: string | null | undefined): string => {
  if (!value) return "";

  const date = new Date(value);
  const days = daysAgo(date);

  if (days === 0) return "today";
  if (days === 1) return "yesterday";
  if (days < 30) return `${days} days ago`;

  return shortDate(date);
};
