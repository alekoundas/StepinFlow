import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { AiChatEventTypeEnum } from "@/shared/enums/backend/ai/ai-chat-event-type-enum";
import { aiKeys } from "@/features/ai/hooks/use-ai";
import { localChatEvent, useAiChatStore } from "@/features/ai/store/ai-chat-store";
import type { AiChatContext, AiChatItem } from "@/features/ai/store/ai-chat-store";
import type { AiChatEventDto, AiChatMessageDto } from "@/shared/models/ai-chat-dtos";

// Ollama starts, stops and loses models without telling anybody, and Settings changes which one answers.
const CHECK_EVERY_MILLISECONDS = 15_000;

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
    refetchInterval: isEnabled ? CHECK_EVERY_MILLISECONDS : false,
  });
}

/** What a question about this execution could show the model. Empty when it may not be shown any. */
export function useAiChatImages(executionId: number | undefined, isEnabled: boolean) {
  return useQuery({
    queryKey: aiKeys.chatImages(executionId ?? 0),
    queryFn: () => backendApiService.Ai.getChatImages(executionId!),
    enabled: isEnabled && !!executionId,
    staleTime: 0,
  });
}

/** Mounted once, around every page: an answer goes on being written while the panel is closed. */
export function useAiChatEvents() {
  const applyEvent = useAiChatStore((x) => x.applyEvent);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.AI_CHAT_EVENT) return;

      applyEvent(message.payload as AiChatEventDto);
    });

    return () => {
      unsubscribe?.();
    };
  }, [applyEvent]);
}

// Everything said before this answer. A failed answer said nothing, and one stopped before its first word neither.
const historyBefore = (items: AiChatItem[], answerId: string): AiChatMessageDto[] => {
  const history: AiChatMessageDto[] = [];

  for (const item of items) {
    if (item.id === answerId) break;

    if (item.kind === "question") history.push({ role: "user", text: item.text });
    else if (item.text) history.push({ role: "assistant", text: item.text });
  }

  return history;
};

const startAnswer = async (conversationId: string, answerId: string, context: AiChatContext) => {
  const { conversations, applyEvent } = useAiChatStore.getState();
  const conversation = conversations.find((x) => x.id === conversationId);
  if (!conversation) return;

  try {
    await backendApiService.Ai.ask({
      answerId: answerId,
      messages: historyBefore(conversation.items, answerId),
      ...context,
    });
  } catch (error) {
    applyEvent(
      localChatEvent(
        answerId,
        AiChatEventTypeEnum.FAILED,
        "The question could not be sent.",
        error instanceof Error ? error.message : "",
      ),
    );
  }
};

/**
 * The history lives in the store and goes out in full every time, so the backend keeps no session
 * and there is nothing to expire, resume or clean up. Read from the store rather than a render
 * closure: a conversation started a moment ago is not in this component's props yet.
 */
export function useAskAi() {
  const ask = (conversationId: string, question: string, context: AiChatContext) => {
    const answerId = crypto.randomUUID();

    useAiChatStore.getState().addExchange(conversationId, question, context, answerId);
    void startAnswer(conversationId, answerId, context);
  };

  /** From a button on a page: the panel opens on a conversation of its own. */
  const askInNewConversation = (question: string, context: AiChatContext) => {
    const { open, startConversation } = useAiChatStore.getState();

    open();
    ask(startConversation(), question, context);
  };

  const askAgain = (conversationId: string, answerId: string) => {
    const { conversations, restartAnswer } = useAiChatStore.getState();
    const items = conversations.find((x) => x.id === conversationId)?.items ?? [];

    const question = items[items.findIndex((x) => x.id === answerId) - 1];
    if (question?.kind !== "question") return;

    const newAnswerId = crypto.randomUUID();

    restartAnswer(conversationId, answerId, newAnswerId);
    void startAnswer(conversationId, newAnswerId, question.context);
  };

  // Ended here at once, so a click never waits on the model. The backend's own word is ignored when it arrives.
  const stop = (answerId: string) => {
    useAiChatStore.getState().applyEvent(localChatEvent(answerId, AiChatEventTypeEnum.STOPPED));
    void backendApiService.Ai.stopAnswer(answerId);
  };

  return { ask, askInNewConversation, askAgain, stop };
}
