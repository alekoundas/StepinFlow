/**
 * What the chosen provider can be asked for. Empty with an error when it could not be reached,
 * which for Ollama usually means it is not running.
 */
export interface AiModelsDto {
  models: string[];
  error: string;
}
