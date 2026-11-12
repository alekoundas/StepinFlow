import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowInputDto } from "@/shared/models/database/flow-input-dto";

// Mutation CRUD
export function useFlowInputMutations() {
  const queryClient = useQueryClient();

  // The inputs come with the flow, and a renamed one can leave a step typing a name nothing has.
  // Awaited, so a save resolves once the lists show it.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["flow"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

  const createFlowInputMutation = useMutation({
    mutationFn: (dto: FlowInputDto) => backendApiService.FlowInput.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowInputMutation = useMutation({
    mutationFn: (dto: FlowInputDto) => backendApiService.FlowInput.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowInputMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowInput.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowInputMutation,
    updateFlowInputMutation,
    deleteFlowInputMutation,
  };
}
