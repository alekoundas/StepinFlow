import {
  hashKey,
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import type {
  FlowStepDisabledDto,
  FlowStepMoveDto,
  FlowStepPasteDto,
} from "@/shared/models/flow-step-move.dto";

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
  // validation badges are refetched alongside the step itself. Awaited, so a save resolves once
  // the form can start again from what was saved.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
      queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
    ]);

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

  // The deleted step stays on screen until the caller lets go of it, so it is left out of the
  // refetch: asking for it again fails, and the retries held the delete up for seven seconds.
  const deleteFlowStepMutation = useMutation({
    mutationFn: (id: number) => backendApiService.FlowStep.delete(id),
    onSuccess: (_, id) => {
      const deleted = hashKey(flowStepKeys.detail(id));

      return Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["flowStep"],
          predicate: (query) => query.queryHash !== deleted,
        }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
      ]);
    },
  });

  // A move rewrites more than the step it moved: reparenting clears the search results that
  // steps below it can no longer reach, so the cached details of the whole flow are suspect.
  const moveFlowStepMutation = useMutation({
    mutationFn: (dto: FlowStepMoveDto) => backendApiService.FlowStep.move(dto),
    onSuccess: invalidate,
  });

  // A copy can carry references to steps outside it that its new place cannot reach.
  const pasteFlowStepMutation = useMutation({
    mutationFn: (dto: FlowStepPasteDto) => backendApiService.FlowStep.paste(dto),
    onSuccess: invalidate,
  });

  // What is wrong with a disabled step stops counting, and comes back with it.
  const setFlowStepDisabledMutation = useMutation({
    mutationFn: (dto: FlowStepDisabledDto) => backendApiService.FlowStep.setDisabled(dto),
    onSuccess: invalidate,
  });

  return {
    createFlowStepMutation,
    updateFlowStepMutation,
    deleteFlowStepMutation,
    moveFlowStepMutation,
    pasteFlowStepMutation,
    setFlowStepDisabledMutation,
  };
}
