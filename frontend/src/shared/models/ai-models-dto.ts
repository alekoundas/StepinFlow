/** One model and what it can do, as the provider reports it. */
export interface AiModelDto {
  name: string;

  /** As the provider names them: completion, tools, vision, thinking, embedding, insert. */
  capabilities: string[];

  /** Tokens the model can hold at once. Zero when the provider did not say. */
  contextLength: number;

  /** Why StepinFlow cannot use it, empty when it can. Every AI feature wants tools and vision. */
  unusableReason: string;
}

/**
 * What can be chosen: the models downloaded into Ollama, and the cloud provider's. Models is empty
 * with an error when Ollama could not be reached, which usually means it is not running.
 */
export interface AiModelsDto {
  models: AiModelDto[];
  cloudModels: AiModelDto[];
  error: string;
}
