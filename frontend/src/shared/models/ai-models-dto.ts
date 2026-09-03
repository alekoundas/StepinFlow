/** One model and what it can do, as the provider reports it. */
export interface AiModelDto {
  name: string;

  /** As the provider names them: completion, tools, vision, thinking, embedding, insert. */
  capabilities: string[];

  /** Tokens the model can hold at once. Zero when the provider did not say. */
  contextLength: number;
}

/**
 * What the chosen provider can be asked for. Empty with an error when it could not be reached,
 * which for Ollama usually means it is not running.
 */
export interface AiModelsDto {
  models: AiModelDto[];
  error: string;
}
