import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";

export const flowLocationKeys = {
  lookup: (flowId: number) => ["lookup", "flowLocation", flowId] as const,
} as const;

// Mutation CRUD
export function useFlowLocationMutations() {
  const queryClient = useQueryClient();

  // The Flow form edits locations as a field array, so a location created straight from a cursor
  // step has to invalidate the flow too or the next Flow save would treat it as removed.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["lookup", "flowLocation"] });
    queryClient.invalidateQueries({ queryKey: ["flow"] });
  };

  const createFlowLocationMutation = useMutation({
    mutationFn: (dto: FlowLocationDto) =>
      backendApiService.FlowLocation.create(dto),
    onSuccess: invalidate,
  });

  const updateFlowLocationMutation = useMutation({
    mutationFn: (dto: FlowLocationDto) =>
      backendApiService.FlowLocation.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowLocationMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowLocation.delete(id),
    onSuccess: invalidate,
  });

  return {
    createFlowLocationMutation,
    updateFlowLocationMutation,
    deleteFlowLocationMutation,
  };
}
