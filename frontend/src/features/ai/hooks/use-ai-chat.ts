import { useQuery } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import { aiKeys } from "@/features/ai/hooks/use-ai";

/**
 * Whether the chat can be offered. A provider being set up is not enough - the chosen model has to
 * be able to call tools, or it would answer about flows it has never seen.
 */
export function useAiChatAvailability(isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.chatAvailability(),
    queryFn: () => backendApiService.Ai.getChatAvailability(),
    enabled: isEnabled,
    staleTime: 0,
  });
}

/**
 * Sends the whole conversation and appends the answer to it.
 *
 * The history lives in the store and goes out in full every time, so the backend keeps no session
 * and there is nothing to expire, resume or clean up.
 */
export function useAskAi() {
  const { addMessage, setPending, setAnswer, setError } = useAiChatStore();

  const ask = async (conversationId: string, question: string) => {
    // From the store rather than a render closure: a conversation started a moment ago is
    // already in the store and not yet in this component's props.
    const conversation = useAiChatStore
      .getState()
      .conversations.find((x) => x.id === conversationId);
    if (!conversation || conversation.isPending) return;

    const asked = [
      ...conversation.messages,
      { role: "user" as const, text: question },
    ];

    addMessage(conversationId, { role: "user", text: question });
    setPending(conversationId, true);

    try {
      const answer = await backendApiService.Ai.ask({ messages: asked });

      if (answer.error) setError(conversationId, answer.error);
      else setAnswer(conversationId, answer.answer, answer.toolCalls);
    } catch (error) {
      setError(
        conversationId,
        error instanceof Error ? error.message : "The question could not be sent.",
      );
    }
  };

  return { ask };
}
