import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { FlowStepDto } from "@/shared/models/database/flow-step/flow-step-dto";
import { backendApiService } from "@/shared/services/backend-api-service";

export const flowStepKeys = {
  detail: (id: number) => ["flowStep", "detail", id] as const,
} as const;

// ── Query: fetch a single FlowStepDto (cached automatically) ──
export function useFlowStep(id: number | null) {
  return useQuery({
    queryKey: id ? flowStepKeys.detail(id) : ["flowStep", "detail", "disabled"],
    queryFn: () => backendApiService.FlowStep.get(id!),
    enabled: !!id, // only run when we have a real ID
    // staleTime: 5 * 60 * 1000,         // override from global if needed
  });
}

// Mutation CRUD
export function useFlowStepMutations() {
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: (dto: FlowStepDto) => backendApiService.FlowStep.create(dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
    // onError: (err) => {
    //   console.error("Failed to create FlowStep", err);
    // },
  });

  const updateMutation = useMutation({
    mutationFn: (dto: FlowStepDto) => backendApiService.FlowStep.update(dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowStep.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
  });

  return { createMutation, updateMutation, deleteMutation };
}
