import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";

export const flowAreaKeys = {
  lookup: (flowId: number) => ["lookup", "flowArea", flowId] as const,
} as const;

// Mutation CRUD
export function useFlowAreaMutations() {
  const queryClient = useQueryClient();

  // The Flow form edits areas as a field array, so an area created straight from a step has to
  // invalidate the flow too or the next Flow save would treat it as removed.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["lookup", "flowArea"] });
    queryClient.invalidateQueries({ queryKey: ["flow"] });
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] });
  };

  const createFlowAreaMutation = useMutation({
    mutationFn: (dto: FlowAreaDto) =>
      backendApiService.FlowArea.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowAreaMutation = useMutation({
    mutationFn: (dto: FlowAreaDto) =>
      backendApiService.FlowArea.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowAreaMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowArea.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowAreaMutation,
    updateFlowAreaMutation,
    deleteFlowAreaMutation,
  };
}
