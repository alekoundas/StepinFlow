import { useMutation, useQueryClient } from "@tanstack/react-query";
import { BackendApiService } from "@/shared/services/backend-api-service";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";

// Mutation CRUD
export function useFlowAreaMutations() {
  const queryClient = useQueryClient();

  // The areas come with the flow. Deleting one clears it from the steps that used it, so the
  // steps are refetched too, and awaited so a save resolves once the lists show it.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowArea"] }),
      queryClient.invalidateQueries({ queryKey: ["flow"] }),
      queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

  const createFlowAreaMutation = useMutation({
    mutationFn: (dto: FlowAreaDto) =>
      BackendApiService.FlowArea.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowAreaMutation = useMutation({
    mutationFn: (dto: FlowAreaDto) =>
      BackendApiService.FlowArea.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowAreaMutation = useMutation({
    mutationFn: (id: number) => BackendApiService.FlowArea.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowAreaMutation,
    updateFlowAreaMutation,
    deleteFlowAreaMutation,
  };
}
