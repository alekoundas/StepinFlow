import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { ExecutionStartDto } from "@/shared/models/execution-start-dto";

export const executionKeys = {
  detail: (id: number) => ["execution", "detail", id] as const,
  list: (flowId: number) => ["execution", "list", flowId] as const,
  state: () => ["execution", "state"] as const,
  flowSummaries: () => ["execution", "flowSummaries"] as const,
  stepScreenshot: (executionStepId: number) =>
    ["execution", "stepScreenshot", executionStepId] as const,
} as const;

/** Every flow with the shape of its run history, for the executions list. */
export function useFlowExecutionSummaries() {
  return useQuery({
    queryKey: executionKeys.flowSummaries(),
    queryFn: () => backendApiService.Execution.getFlowSummaries(),
  });
}

/** One past run and every step of it. */
export function useExecution(id: number | null) {
  return useQuery({
    queryKey: id ? executionKeys.detail(id) : ["execution", "detail", "disabled"],
    queryFn: () => backendApiService.Execution.get(id!),
    enabled: !!id,
  });
}

/** One screenshot a run left on disk, base64. Null when it is not there any more. */
export function useExecutionStepScreenshot(executionStepId: number | null) {
  return useQuery({
    queryKey: executionStepId
      ? executionKeys.stepScreenshot(executionStepId)
      : ["execution", "stepScreenshot", "disabled"],
    queryFn: () => backendApiService.Execution.getStepScreenshot(executionStepId!),
    enabled: !!executionStepId,
  });
}

/** Past runs of one flow, newest first. */
export function useExecutionList(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? executionKeys.list(flowId) : ["execution", "list", "disabled"],
    queryFn: () => backendApiService.Execution.getList(flowId!),
    enabled: !!flowId,
  });
}

/**
 * What the engine is doing. Read once on mount - after that the broadcast says what changed, so
 * this never polls.
 */
export function useExecutionState() {
  return useQuery({
    queryKey: executionKeys.state(),
    queryFn: () => backendApiService.Execution.getState(),
    staleTime: 0,
  });
}

// Mutation CRUD
export function useExecutionMutations() {
  const queryClient = useQueryClient();

  const startExecutionMutation = useMutation({
    mutationFn: (dto: ExecutionStartDto) => backendApiService.Execution.start(dto),
    onSuccess: (_, dto) => {
      queryClient.invalidateQueries({ queryKey: executionKeys.list(dto.flowId) });
      queryClient.invalidateQueries({ queryKey: executionKeys.state() });
    },
  });

  const stopExecutionMutation = useMutation({
    mutationFn: () => backendApiService.Execution.stop(),
  });

  const pauseExecutionMutation = useMutation({
    mutationFn: () => backendApiService.Execution.pause(),
  });

  const continueExecutionMutation = useMutation({
    mutationFn: () => backendApiService.Execution.continue(),
  });

  const stepIntoExecutionMutation = useMutation({
    mutationFn: () => backendApiService.Execution.stepInto(),
  });

  const stepOverExecutionMutation = useMutation({
    mutationFn: () => backendApiService.Execution.stepOver(),
  });

  const setBreakpointsMutation = useMutation({
    mutationFn: (flowStepIds: number[]) =>
      backendApiService.Execution.setBreakpoints(flowStepIds),
  });

  return {
    startExecutionMutation,
    stopExecutionMutation,
    pauseExecutionMutation,
    continueExecutionMutation,
    stepIntoExecutionMutation,
    stepOverExecutionMutation,
    setBreakpointsMutation,
  };
}
