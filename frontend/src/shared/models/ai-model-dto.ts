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
