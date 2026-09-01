import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type { AiChatMessageDto } from "@/shared/models/ai-chat-dtos";

export interface AiConversation {
  id: string;
  title: string;
  messages: AiChatMessageDto[];

  /** What the last answer looked at. Kept per conversation so switching tabs keeps it. */
  toolCalls: string[];

  isPending: boolean;
  error: string;
}

interface Props {
  isOpen: boolean;
  conversations: AiConversation[];
  activeId: string | undefined;
}

interface Actions {
  open: () => void;
  close: () => void;

  startConversation: () => void;
  closeConversation: (id: string) => void;
  setActive: (id: string) => void;

  addMessage: (id: string, message: AiChatMessageDto) => void;
  setPending: (id: string, isPending: boolean) => void;
  setAnswer: (id: string, text: string, toolCalls: string[]) => void;
  setError: (id: string, error: string) => void;
}

const newConversation = (): AiConversation => ({
  id: crypto.randomUUID(),
  title: "New question",
  messages: [],
  toolCalls: [],
  isPending: false,
  error: "",
});

// The first thing asked names the tab, which is what you would go looking for it by.
const titleOf = (text: string): string =>
  text.length > 32 ? `${text.slice(0, 32).trimEnd()}...` : text;

export const useAiChatStore = create<Props & Actions>()(
  devtools((set) => ({
    isOpen: false,
    conversations: [],
    activeId: undefined,

    open: (): void =>
      set((state) => {
        if (state.conversations.length > 0)
          return { isOpen: true };

        const first = newConversation();
        return { isOpen: true, conversations: [first], activeId: first.id };
      }),

    close: (): void => set({ isOpen: false }),

    startConversation: (): void =>
      set((state) => {
        const next = newConversation();
        return {
          conversations: [...state.conversations, next],
          activeId: next.id,
        };
      }),

    // Closing the last one leaves an empty conversation rather than an empty window.
    closeConversation: (id: string): void =>
      set((state) => {
        const remaining = state.conversations.filter((x) => x.id !== id);

        if (remaining.length === 0) {
          const next = newConversation();
          return { conversations: [next], activeId: next.id };
        }

        return {
          conversations: remaining,
          activeId: state.activeId === id ? remaining[0].id : state.activeId,
        };
      }),

    setActive: (id: string): void => set({ activeId: id }),

    addMessage: (id: string, message: AiChatMessageDto): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id !== id
            ? x
            : {
                ...x,
                messages: [...x.messages, message],
                title:
                  x.messages.length === 0 && message.role === "user"
                    ? titleOf(message.text)
                    : x.title,
                error: "",
              },
        ),
      })),

    setPending: (id: string, isPending: boolean): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id === id ? { ...x, isPending: isPending } : x,
        ),
      })),

    setAnswer: (id: string, text: string, toolCalls: string[]): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id !== id
            ? x
            : {
                ...x,
                messages: [...x.messages, { role: "assistant", text: text }],
                toolCalls: toolCalls,
                isPending: false,
              },
        ),
      })),

    setError: (id: string, error: string): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id === id ? { ...x, error: error, isPending: false } : x,
        ),
      })),
  })),
);
