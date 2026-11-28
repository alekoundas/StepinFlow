import type { FlowDto } from "@/shared/models/database/flow-dto";
import { BackendApiService } from "@/shared/services/backend-api-service";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

const flowKeys = {
  detail: (id: number) => ["flow", "detail", id] as const,
} as const;

function useFlow(id: number | null) {
  return useQuery({
    queryKey: id ? flowKeys.detail(id) : ["flow", "detail", "disabled"],
    queryFn: () => BackendApiService.Flow.get(id!),
    enabled: !!id,
    // staleTime: 5 * 60 * 1000,         // override from global if needed
  });
}

// Mutation CRUD
function useFlowMutations() {
  const queryClient = useQueryClient();

  const createFlowMutation = useMutation({
    mutationFn: (dto: FlowDto) => BackendApiService.Flow.create(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
      // Search area / location usage counts are computed server side.
      queryClient.invalidateQueries({ queryKey: ["flow"] });
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowPoint"] });
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowArea"] });
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] });
    },
  });

  // Awaited, so a save resolves once the form can start again from what was saved.
  const updateFlowMutation = useMutation({
    mutationFn: (dto: FlowDto) => BackendApiService.Flow.update(dto),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["flows", "list"] }),
        // Search area / location usage counts are computed server side.
        queryClient.invalidateQueries({ queryKey: ["flow"] }),
        queryClient.invalidateQueries({ queryKey: ["lookup", "flowPoint"] }),
        queryClient.invalidateQueries({ queryKey: ["lookup", "flowArea"] }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
      ]),
  });

  const deleteFlowMutation = useMutation({
    mutationFn: (id: number) => BackendApiService.Flow.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
      // Its executions went with it.
      queryClient.invalidateQueries({ queryKey: ["execution"] });
      // Search area / location usage counts are computed server side.
      queryClient.invalidateQueries({ queryKey: ["flow"] });
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowPoint"] });
      queryClient.invalidateQueries({ queryKey: ["lookup", "flowArea"] });
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] });
    },
  });

  return { createFlowMutation, updateFlowMutation, deleteFlowMutation };
}

export const FlowHooks = {
  keys: flowKeys,
  useFlow,
  useFlowMutations,
};
