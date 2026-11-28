import { useQuery } from "@tanstack/react-query";

import { BackendApiService } from "@/shared/services/backend-api-service";

const aiKeys = {
  all: ["ai"] as const,
  status: () => ["ai", "status"] as const,
  localModel: () => ["ai", "localModel"] as const,
  models: () => ["ai", "models"] as const,
  suggestions: () => ["ai", "suggestions"] as const,
  downloadState: () => ["ai", "downloadState"] as const,
  chatAvailability: () => ["ai", "chatAvailability"] as const,
  chatImages: (executionId: number) => ["ai", "chatImages", executionId] as const,
} as const;

/** What a page says in place of an AI feature while useAiStatus is false. */
const AI_OFF =
  "AI is off until Ollama is running on this machine with a model downloaded. Choose the model in Settings.";

/**
 * Whether Ollama answers with the local model chosen in Settings, which every AI feature needs.
 * Read once so a page can offer the feature or explain why it is not there, rather than letting
 * somebody click and get an error.
 */
function useAiStatus() {
  return useQuery({
    queryKey: aiKeys.status(),
    queryFn: () => BackendApiService.Ai.getStatus(),
    staleTime: 0,
  });
}

/** The models downloaded into Ollama, and the cloud provider's. */
function useAiModels(isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.models(),
    queryFn: () => BackendApiService.Lookup.aiModels(),
    enabled: isEnabled,
    staleTime: 0,
  });
}

/**
 * Local models worth offering to somebody who has none, with the ones already downloaded marked.
 */
function useAiModelSuggestions(isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.suggestions(),
    queryFn: () => BackendApiService.Lookup.aiModelSuggestions(),
    enabled: isEnabled,
    staleTime: 0,
  });
}

export const AiHooks = {
  keys: aiKeys,
  AI_OFF,
  useAiStatus,
  useAiModels,
  useAiModelSuggestions,
};
