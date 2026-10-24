import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";
import type { FlowTemplateDto } from "@/shared/models/database/flow-template-dto";

// Under "flowStep", so every step save refreshes them: what a step uses changes with the step.
export const flowPartKeys = {
  stepUses: (flowId: number) => ["flowStep", "uses", flowId] as const,
  templates: (flowId: number) => ["flowStep", "templates", flowId] as const,
  script: (flowId: number) => ["flowScript", "render", flowId] as const,
} as const;

/** What each step of the flow uses. The source of every "Used by" in the editor's tabs. */
export function useFlowStepUses(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.stepUses(flowId) : ["flowStep", "uses", "disabled"],
    queryFn: () => backendApiService.Flow.getStepUses(flowId!),
    enabled: !!flowId,
  });
}

/** The flow's pictures, with their images and how many steps link to each. */
export function useFlowTemplates(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.templates(flowId) : ["flowStep", "templates", "disabled"],
    queryFn: () => backendApiService.FlowTemplate.getByFlow(flowId!),
    enabled: !!flowId,
  });
}

/** Saves a picture on its own. Every step that links to it, and the script, change with it. */
export function useUpdateFlowTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: FlowTemplateDto) => backendApiService.FlowTemplate.update(dto),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
        queryClient.invalidateQueries({ queryKey: ["flowScript"] }),
      ]),
  });
}

/** The script Export would write. Rendered again each time the Script tab opens. */
export function useFlowScript(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowPartKeys.script(flowId) : ["flowScript", "render", "disabled"],
    queryFn: () => backendApiService.FlowScript.render(flowId!),
    enabled: !!flowId,
    staleTime: 0,
  });
}

/** The steps among these that use a part, by the test given. */
export const stepsUsing = (
  uses: FlowStepUseDto[] | undefined,
  isUsing: (use: FlowStepUseDto) => boolean,
): FlowStepUseDto[] => (uses ?? []).filter(isUsing);

/** The names of the steps that search for a picture, leaving out the one asking. */
export const stepsSearchingFor = (
  uses: FlowStepUseDto[] | undefined,
  templateName: string,
  exceptFlowStepId?: number,
): string[] =>
  (uses ?? [])
    .filter((x) => x.flowStepId !== exceptFlowStepId && x.templateNames.includes(templateName))
    .map((x) => x.name);

/** Whether a step fills in this input. Names match the way the execution matches them. */
export const typesInput = (use: FlowStepUseDto, name: string): boolean =>
  use.variableNames.some((x) => x.toLowerCase() === name.toLowerCase());
