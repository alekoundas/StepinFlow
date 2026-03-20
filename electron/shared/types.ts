export interface RequestMessage {
  action: string;
  payload: unknown;
  correlationId?: string;
}

export interface ResponseMessage<T> {
  action: string;
  payload: T;
  correlationId: string;
  error?: string;
}

export type KnownActions = "greet" | "test" | "load-flow" | "save-config";

export interface RequestPayloads {
  greet: { name: string };
  test: Record<string, never>; // empty object
}

export interface ResponsePayloads {
  greet: { greeting: string };
}
