import { useMutation, useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";

export const aiKeys = {
  status: () => ["ai", "status"] as const,
  models: () => ["ai", "models"] as const,
  suggestions: () => ["ai", "suggestions"] as const,
  downloadState: () => ["ai", "downloadState"] as const,
  chatAvailability: () => ["ai", "chatAvailability"] as const,
} as const;

/** What a page says in place of an AI feature while useAiStatus is false. */
export const AI_OFF =
  "AI is off until Ollama is running on this machine with a model downloaded. Choose the model in Settings.";

/**
 * Whether Ollama answers with the local model chosen in Settings, which every AI feature needs.
 * Read once so a page can offer the feature or explain why it is not there, rather than letting
 * somebody click and get an error.
 */
export function useAiStatus() {
  return useQuery({
    queryKey: aiKeys.status(),
    queryFn: () => backendApiService.Ai.getStatus(),
    staleTime: 0,
  });
}

/** The models downloaded into Ollama, and the cloud provider's. */
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
