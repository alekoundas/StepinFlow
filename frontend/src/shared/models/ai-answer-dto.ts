export interface AiAnswerDto {
  answer: string;
  prompt: string;

  /** Set when the model could not be reached. The answer is empty when this is not. */
  error: string;
}
