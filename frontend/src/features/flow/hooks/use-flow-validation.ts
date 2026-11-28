import { useQuery } from "@tanstack/react-query";
import { BackendApiService } from "@/shared/services/backend-api-service";

const flowValidationKeys = {
  all: ["flowValidation"] as const,
  detail: (flowId: number) => ["flowValidation", flowId] as const,
} as const;

/**
 * One answer for the whole flow, shared by everything that shows it. The tree badges its rows
 * from this and the execution page blocks Start from the same list - the list the backend refuses
 * Start on - so they cannot end up disagreeing about whether a flow is runnable.
 */
export function useFlowValidation(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowValidationKeys.detail(flowId) : ["flowValidation", "disabled"],
    queryFn: () => BackendApiService.Flow.validate(flowId!),
    enabled: !!flowId,
  });
}
