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

    onSuccess: (result, variables) => {
      // Invalidate all flow-step caches so VIEW mode sees fresh data
      queryClient.invalidateQueries({ queryKey: ["flowStep"] });

      // The tree refresh trigger you already have still works
      // (we’ll call it from the component)
    },

    onError: (err) => {
      console.error("Failed to create FlowStep", err);
      // TODO: add toast later
    },
  });

  // Update is not yet in your backend-api-service, so placeholder
  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: FlowStepDto }) => {
      // TODO: add this method to backendApiService.FlowStep when you implement the backend endpoint
      // For now it does nothing
      console.warn("FlowStep.update not implemented yet in backend");
      return Promise.resolve({ success: true });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
  });

  return { createMutation, updateMutation };
}
