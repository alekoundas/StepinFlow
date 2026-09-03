import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import StatusPillComponent, {
  type StatusPillSeverity,
} from "@/shared/components/StatusPillComponent";
import RunSummaryComponent from "@/features/execution/components/RunSummaryComponent";
import { useAiStatus } from "@/features/ai/hooks/use-ai";
import { useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { ExecutionDto } from "@/shared/models/database/execution-dto";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

interface Props {
  flowName: string;

  /** The run being read. Absent while a live run has not been opened from the executions list. */
  execution?: ExecutionDto;
  executionSteps: ExecutionStepDto[];

  /** History being read back, rather than a run this page can drive. */
  isPastRun: boolean;
}

/**
 * Which run this is and how it went, in the space a page title used to take.
 *
 * The old header was the flow name at 5xl and nothing else, so the two questions asked on arrival -
 * which run am I looking at, and did it work - were answered by scrolling.
 */
export default function RunHeaderComponent({
  flowName,
  execution,
  executionSteps,
  isPastRun,
}: Props) {
  const navigate = useNavigate();

  return (
    <div className="flex flex-column gap-2">
      <div className="flex flex-wrap align-items-center gap-3">
        <div
          className="flex align-items-center gap-1 cursor-pointer"
          onClick={() => navigate("/executions")}
        >
          <LabelComponent
            text="Executions"
            size="sm"
            color="secondary"
          />
          <IconComponent
            name="angle-right"
            size="sm"
            className="text-color-secondary"
          />
        </div>

        <LabelComponent
          text={flowName}
          size="2xl"
          weight="bold"
        />

        {execution ? (
          <>
            <LabelComponent
              text={`Run #${execution.id}`}
              size="lg"
              color="secondary"
            />
            <StatusPillComponent
              text={execution.status}
              severity={statusSeverity(execution.status)}
              pulse={execution.status === ExecutionStatusEnum.RUNNING}
            />
          </>
        ) : null}

        <div className="flex-1" />

        {/* A finished run is something to ask about, not something to drive. */}
        {isPastRun && execution ? (
          <AskAiButtons
            execution={execution}
            flowName={flowName}
          />
        ) : null}
      </div>

      <RunSummaryComponent
        executionSteps={executionSteps}
        errorFlowStepId={execution?.errorFlowStepId}
      />
    </div>
  );
}

interface AskAiButtonsProps {
  execution: ExecutionDto;
  flowName: string;
}

/**
 * Both open the same chat, seeded differently.
 *
 * Explain used to be a tab holding one answer with nowhere to go. A run raises "why", then "what
 * else could it be" and "has it always done this", and only a conversation takes those.
 */
function AskAiButtons({ execution, flowName }: AskAiButtonsProps) {
  const { data: isConfigured } = useAiStatus();
  const { open, startConversation } = useAiChatStore();
  const { ask } = useAskAi();

  if (!isConfigured) return null;

  const askAbout = (question: string) => {
    const conversationId = startConversation();
    open();

    // The run goes with it, so a model that can see is shown what the failing step saw.
    ask(conversationId, question, execution.id);
  };

  return (
    <div className="flex align-items-center gap-2">
      <Button
        label="Explain this run"
        icon="pi pi-sparkles"
        size="small"
        onClick={() =>
          askAbout(
            `Explain run ${execution.id} of the flow "${flowName}". What happened, why did it end the way it did, and what should I change?`,
          )
        }
      />
      <Button
        label="Ask about this run"
        icon="pi pi-comments"
        size="small"
        outlined
        onClick={() => askAbout(`I am looking at run ${execution.id} of "${flowName}". `)}
      />
    </div>
  );
}

function statusSeverity(status: ExecutionStatusEnum): StatusPillSeverity {
  switch (status) {
    case ExecutionStatusEnum.RUNNING:
      return "running";
    case ExecutionStatusEnum.COMPLETED:
      return "success";
    case ExecutionStatusEnum.ERRORED:
      return "danger";
    default:
      return "paused";
  }
}
