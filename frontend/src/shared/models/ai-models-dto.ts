import type { AiModelDto } from "@/shared/models/ai-model-dto";

/**
 * What can be chosen: the models downloaded into Ollama, and the cloud provider's. Models is empty
 * with an error when Ollama could not be reached, which usually means it is not running.
 */
export interface AiModelsDto {
  models: AiModelDto[];
  cloudModels: AiModelDto[];
  error: string;
}
