import { memo } from "react";
import { Link } from "react-router-dom";
import { Button } from "primereact/button";

import StatusPillComponent, {
  type StatusPillSeverity,
} from "@/shared/components/StatusPillComponent";
import ExecutionToolbarComponent from "@/features/execution/components/ExecutionToolbarComponent";
import ExecutedVersionComponent from "@/features/repository/components/ExecutedVersionComponent";
import { useAiStatus } from "@/features/ai/hooks/use-ai";
import { useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { MONO, MUTED } from "@/features/execution/execution-format";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import { executionStatusSeverity, executionStatusText } from "@/shared/utils/execution-status";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";

const KEEPS_TEXT: Record<ExecutionHistoryLevelEnum, string> = {
  [ExecutionHistoryLevelEnum.STEPS_AND_IMAGES]: "Keeps steps and images",
  [ExecutionHistoryLevelEnum.STEPS]: "Keeps steps only",
  [ExecutionHistoryLevelEnum.NONE]: "Keeps nothing",
};

interface Props {
  flowId: number;
  flowName: string;

  /** The execution being read. Absent while a live one is followed through the store. */
  execution?: ExecutionDto;

  /** History being read back, rather than an execution this page can drive. */
  isPastExecution: boolean;
}

/** Which execution this is and how it is going, with the debugger beside it. */
function ExecutionHeaderComponent({
  flowId,
  flowName,
  execution,
  isPastExecution,
}: Props) {
  const runState = useExecutionStore((x) => x.runState);
  const liveExecutionId = useExecutionStore((x) => x.executionId);
  const endedStatus = useExecutionStore((x) => x.endedStatus);
  const liveHistoryLevel = useExecutionStore((x) => x.historyLevel);

  const executionId = isPastExecution ? execution?.id : liveExecutionId;

  let pill: Pill | undefined;
  if (isPastExecution) pill = execution ? statusPill(execution.status) : undefined;
  else pill = livePill(runState, endedStatus);

  // Before a start the dropdown says it, so only an execution under way or over.
  let keeps: ExecutionHistoryLevelEnum | undefined;
  if (isPastExecution) keeps = execution?.historyLevel;
  else if (pill) keeps = liveHistoryLevel;

  return (
    <div className="flex flex-column gap-3">
      <div className="flex flex-wrap align-items-center justify-content-between gap-3">
        <nav
          aria-label="Breadcrumb"
          className="flex align-items-center gap-2 text-sm"
          style={MUTED}
        >
          <Link
            to="/executions"
            style={{ color: "inherit", textDecoration: "none" }}
          >
            Executions
          </Link>
          <i
            className="pi pi-angle-right text-xs"
            aria-hidden="true"
          />
          <span style={{ color: "var(--text-color)" }}>{flowName}</span>
        </nav>

        <div
          className="flex flex-wrap align-items-center gap-2 text-xs"
          style={MUTED}
        >
          {keeps ? <span>{KEEPS_TEXT[keeps]}</span> : null}
          {execution ? (
            <ExecutedVersionComponent
              commitSha={execution.commitSha}
              hasUncommittedChanges={execution.hasUncommittedChanges}
            />
          ) : null}
        </div>
      </div>

      <div className="flex flex-wrap align-items-center justify-content-between gap-3">
        <div className="flex flex-wrap align-items-center gap-3">
          <h1 className="m-0 text-3xl font-semibold">{flowName}</h1>

          {pill ? (
            <StatusPillComponent
              text={pill.text}
              severity={pill.severity}
              pulse={pill.pulse}
            />
          ) : null}

          {executionId ? (
            <span
              className="text-sm"
              style={{ ...MUTED, fontFamily: MONO }}
            >
              Execution {executionId}
            </span>
          ) : null}
        </div>

        {/* A past execution has already happened. Offering to start, pause or step it is a lie. */}
        {isPastExecution ? (
          execution ? (
            <AskAiButtons
              execution={execution}
              flowName={flowName}
            />
          ) : null
        ) : (
          <ExecutionToolbarComponent flowId={flowId} />
        )}
      </div>
    </div>
  );
}

// Not redrawn by every batch of rows the page draws for.
export default memo(ExecutionHeaderComponent);

interface Pill {
  text: string;
  severity: StatusPillSeverity;
  pulse: boolean;
}

function statusPill(status: ExecutionStatusEnum): Pill {
  return {
    text: executionStatusText(status),
    severity: executionStatusSeverity(status),
    pulse: status === ExecutionStatusEnum.RUNNING,
  };
}

function livePill(
  runState: RunStateEnum,
  endedStatus: ExecutionStatusEnum | undefined,
): Pill | undefined {
  switch (runState) {
    case RunStateEnum.RUNNING:
      return { text: "Running", severity: "running", pulse: true };
    case RunStateEnum.PAUSED:
      return { text: "Paused", severity: "paused", pulse: false };
    case RunStateEnum.STOPPING:
      return { text: "Stopping", severity: "danger", pulse: false };
    default:
      return endedStatus ? statusPill(endedStatus) : undefined;
  }
}

interface AskAiButtonsProps {
  execution: ExecutionDto;
  flowName: string;
}

/**
 * Both open the same chat, seeded differently.
 *
 * Explain used to be a tab holding one answer with nowhere to go. An execution raises "why", then
 * "what else could it be" and "has it always done this", and only a conversation takes those.
 */
function AskAiButtons({ execution, flowName }: AskAiButtonsProps) {
  const { data: isConfigured } = useAiStatus();
  const { open, startConversation } = useAiChatStore();
  const { ask } = useAskAi();

  if (!isConfigured) return null;

  const askAbout = (question: string) => {
    const conversationId = startConversation();
    open();

    // The execution goes with it, so a model that can see is shown what the failing step saw.
    ask(conversationId, question, execution.id);
  };

  return (
    <div className="flex align-items-center gap-2">
      <Button
        label="Explain this execution"
        icon="pi pi-sparkles"
        size="small"
        onClick={() =>
          askAbout(
            `Explain execution ${execution.id} of the flow "${flowName}". What happened, why did it end the way it did, and what should I change?`,
          )
        }
      />
      <Button
        label="Ask about this execution"
        icon="pi pi-comments"
        size="small"
        outlined
        onClick={() => askAbout(`I am looking at execution ${execution.id} of "${flowName}". `)}
      />
    </div>
  );
}
