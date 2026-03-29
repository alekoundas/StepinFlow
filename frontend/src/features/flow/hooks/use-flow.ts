import type { FlowDto } from "@/shared/models/database/flow/flow-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export const flowKeys = {
  detail: (id: number) => ["flow", "detail", id] as const,
} as const;

export function useFlow(id: number | null) {
  return useQuery({
    queryKey: id ? flowKeys.detail(id) : ["flow", "detail", "disabled"],
    queryFn: () => backendApiService.Flow.get(id!),
    enabled: !!id,
    // staleTime: 5 * 60 * 1000,         // override from global if needed
  });
}

// Mutation CRUD
export function useFlowMutations() {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: (dto: FlowDto) => backendApiService.Flow.create(dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flow"] }),
  });

  const updateMutation = useMutation({
    mutationFn: (dto: FlowDto) => backendApiService.Flow.update(dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flow"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => backendApiService.Flow.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flow"] }),
  });

  return { createMutation, updateMutation, deleteMutation };
}
