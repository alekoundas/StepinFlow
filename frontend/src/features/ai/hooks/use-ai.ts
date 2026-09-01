import { useMutation, useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";

export const aiKeys = {
  status: () => ["ai", "status"] as const,
  models: () => ["ai", "models"] as const,
  suggestions: () => ["ai", "suggestions"] as const,
  downloadState: () => ["ai", "downloadState"] as const,
  chatAvailability: () => ["ai", "chatAvailability"] as const,
} as const;

/**
 * Whether a provider is set up. Read once so a page can offer the feature or explain why it is
 * not there, rather than letting somebody click and get an error.
 */
export function useAiStatus() {
  return useQuery({
    queryKey: aiKeys.status(),
    queryFn: () => backendApiService.Ai.getStatus(),
    staleTime: 0,
  });
}

/**
 * What the chosen provider offers. Keyed on nothing but the provider setting, so changing provider
 * and invalidating settings brings a different list back.
 */
export function useAiModels(isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.models(),
    queryFn: () => backendApiService.Lookup.aiModels(),
    enabled: isEnabled,
    staleTime: 0,
  });
}

/**
 * Local models worth offering to somebody who has none, with the ones already downloaded marked.
 */
export function useAiModelSuggestions(isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.suggestions(),
    queryFn: () => backendApiService.Lookup.aiModelSuggestions(),
    enabled: isEnabled,
    staleTime: 0,
  });
}

export function useAiMutations() {
  const explainExecutionMutation = useMutation({
    mutationFn: (executionId: number) =>
      backendApiService.Ai.explainExecution(executionId),
  });

  return { explainExecutionMutation };
}
