import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BackendApiService } from "@/shared/services/backend-api-service";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";

// Under "flowStep", so every step save refreshes them: what a step uses changes with the step.
const flowPartKeys = {
  stepUses: (flowId: number) => ["flowStep", "uses", flowId] as const,
  templates: (flowId: number) => ["flowStep", "templates", flowId] as const,
  script: (flowId: number) => ["flowScript", "render", flowId] as const,
} as const;

/** What each step of the flow uses. The source of every "Used by" in the editor's tabs. */
function useFlowStepUses(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.stepUses(flowId) : ["flowStep", "uses", "disabled"],
    queryFn: () => BackendApiService.Flow.getStepUses(flowId!),
    enabled: !!flowId,
  });
}

/** The flow's pictures, with their images and how many steps link to each. */
function useFlowTemplates(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.templates(flowId) : ["flowStep", "templates", "disabled"],
    queryFn: () => BackendApiService.FlowTemplate.getByFlow(flowId!),
    enabled: !!flowId,
  });
}

/** Saves a picture on its own. Every step that links to it, and the script, change with it. */
function useUpdateFlowTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: FlowTemplateDto) => BackendApiService.FlowTemplate.update(dto),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
        queryClient.invalidateQueries({ queryKey: ["flowScript"] }),
      ]),
  });
}

/** The script Export would write. Rendered again each time the Script tab opens. */
function useFlowScript(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.script(flowId) : ["flowScript", "render", "disabled"],
    queryFn: () => BackendApiService.FlowScript.render(flowId!),
    enabled: !!flowId,
    staleTime: 0,
  });
}

export const FlowPartHooks = {
  useFlowStepUses,
  useFlowTemplates,
  useUpdateFlowTemplate,
  useFlowScript,
};
