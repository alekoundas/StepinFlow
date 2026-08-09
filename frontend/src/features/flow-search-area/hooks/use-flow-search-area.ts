import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";

export const flowSearchAreaKeys = {
  lookup: (flowId: number) => ["lookup", "flowSearchArea", flowId] as const,
} as const;

// Mutation CRUD
export function useFlowSearchAreaMutations() {
  const queryClient = useQueryClient();

  // The Flow form edits areas as a field array, so an area created straight from a step has to
  // invalidate the flow too or the next Flow save would treat it as removed.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["lookup", "flowSearchArea"] });
    queryClient.invalidateQueries({ queryKey: ["flow"] });
  };

  const createFlowSearchAreaMutation = useMutation({
    mutationFn: (dto: FlowSearchAreaDto) =>
      backendApiService.FlowSearchArea.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowSearchAreaMutation = useMutation({
    mutationFn: (dto: FlowSearchAreaDto) =>
      backendApiService.FlowSearchArea.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowSearchAreaMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowSearchArea.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowSearchAreaMutation,
    updateFlowSearchAreaMutation,
    deleteFlowSearchAreaMutation,
  };
}
