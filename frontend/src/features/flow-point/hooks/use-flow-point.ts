import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";

export const flowPointKeys = {
  lookup: (flowId: number) => ["lookup", "flowPoint", flowId] as const,
} as const;

// Mutation CRUD
export function useFlowPointMutations() {
  const queryClient = useQueryClient();

  // The Flow form edits locations as a field array, so a location created straight from a cursor
  // step has to invalidate the flow too or the next Flow save would treat it as removed.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["lookup", "flowPoint"] });
    queryClient.invalidateQueries({ queryKey: ["flow"] });
  };

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
