import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";

export const flowPointKeys = {
  lookup: (flowId: number) => ["lookup", "flowPoint", flowId] as const,
} as const;

// Mutation CRUD
export function useFlowPointMutations() {
  const queryClient = useQueryClient();

  // The locations come with the flow. Deleting one clears it from the steps that used it, so the
  // steps are refetched too, and awaited so a save resolves once the lists show it.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowPoint"] }),
      queryClient.invalidateQueries({ queryKey: ["flow"] }),
      queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

  const createFlowPointMutation = useMutation({
    mutationFn: (dto: FlowPointDto) =>
      backendApiService.FlowPoint.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowPointMutation = useMutation({
    mutationFn: (dto: FlowPointDto) =>
      backendApiService.FlowPoint.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowPointMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowPoint.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowPointMutation,
    updateFlowPointMutation,
    deleteFlowPointMutation,
  };
}
