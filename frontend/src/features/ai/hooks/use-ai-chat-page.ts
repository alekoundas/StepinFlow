import { matchPath, useLocation } from "react-router-dom";

import { useExecution } from "@/features/execution/hooks/use-execution";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useFlow } from "@/features/flow/hooks/use-flow";
import { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";

export type AiChatPageKind = "execution" | "flow" | "none";

export interface AiChatPage {
  /** Which page this is. A chip taken off stays off until this changes. */
  key: string;
  kind: AiChatPageKind;
  label: string;
  flowId?: number;
  executionId?: number;
  didFail: boolean;
}

/** What the user is looking at, read off the address so it follows them from page to page. */
export function useAiChatPage(): AiChatPage {
  const { pathname, search } = useLocation();
  const liveExecutionId = useExecutionStore((x) => x.executionId);
  const endedStatus = useExecutionStore((x) => x.endedStatus);

  const onExecution = matchPath("/execution/:id", pathname);
  const onFlow = matchPath("/workflow/:id", pathname);

  const flowId = Number(onExecution?.params.id ?? onFlow?.params.id) || undefined;

  const requested = onExecution ? new URLSearchParams(search).get("executionId") : null;
  const pastExecutionId = requested ? +requested : undefined;
  const executionId = onExecution ? (pastExecutionId ?? liveExecutionId) : undefined;

  const { data: flow } = useFlow(flowId ?? null);
  const { data: pastExecution } = useExecution(pastExecutionId ?? null);

  const flowName = flow?.name ?? "This flow";

  if (executionId) {
    const status = pastExecutionId ? pastExecution?.status : endedStatus;

    return {
      key: `execution:${executionId}`,
      kind: "execution",
      label: `Execution ${executionId} · ${flowName}`,
      flowId: flowId,
      executionId: executionId,
      didFail: status === ExecutionStatusEnum.FAILED || status === ExecutionStatusEnum.ERRORED,
    };
  }

  if (flowId) return { key: `flow:${flowId}`, kind: "flow", label: flowName, flowId: flowId, didFail: false };

  return { key: "none", kind: "none", label: "", didFail: false };
}
