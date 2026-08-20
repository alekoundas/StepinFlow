import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type { FlowStepMoveDto } from "@/shared/models/flow-step-move.dto";

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

  // Anything that changes a step can change what is broken about the flow around it, so the
  // validation badges are refetched alongside the step itself.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["flowStep"] });
    queryClient.invalidateQueries({ queryKey: ["flowValidation"] });
  };

  const createFlowStepMutation = useMutation({
    mutationFn: (dto: FlowStepDto) => backendApiService.FlowStep.create(dto),
    onSuccess: invalidate,
    // onError: (err) => {
    //   console.error("Failed to create FlowStep", err);
    // },
  });

  const updateFlowStepMutation = useMutation({
    mutationFn: (dto: FlowStepDto) => backendApiService.FlowStep.update(dto),
    onSuccess: invalidate,
  });

  const deleteFlowStepMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowStep.delete(id),
    onSuccess: invalidate,
  });

  // A move rewrites more than the step it moved: reparenting clears the search results that
  // steps below it can no longer reach, so the cached details of the whole flow are suspect.
  const moveFlowStepMutation = useMutation({
    mutationFn: (dto: FlowStepMoveDto) => backendApiService.FlowStep.move(dto),
    onSuccess: invalidate,
  });

  return {
    createFlowStepMutation,
    updateFlowStepMutation,
    deleteFlowStepMutation,
    moveFlowStepMutation,
  };
}
