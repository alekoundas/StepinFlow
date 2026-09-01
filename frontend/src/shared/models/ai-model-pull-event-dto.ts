/**
 * How a download is going. Arrives on the broadcast pipe rather than as a reply, because pulling a
 * model is minutes and gigabytes and a request that waited for it would time out.
 */
export interface AiModelPullEventDto {
  model: string;

  /** Ollama's own wording - "pulling manifest", "verifying sha256 digest". */
  status: string;

  completed: number;
  total: number;

  isDone: boolean;
  error: string;
}
