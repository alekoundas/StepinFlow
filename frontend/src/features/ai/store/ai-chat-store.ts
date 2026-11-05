import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { AiChatEventTypeEnum } from "@/shared/enums/backend/ai/ai-chat-event-type-enum";
import type { AiChatEventDto, AiChatStepReferenceDto } from "@/shared/models/ai-chat-dtos";

export interface AiChatContext {
  flowId?: number;
  executionId?: number;
  flowStepId?: number;
  sendScreenshots: boolean;
}

export interface AiChatQuestion {
  kind: "question";
  id: string;
  text: string;

  /** Kept so asking again asks the same thing. */
  context: AiChatContext;
}

export interface AiChatActivity {
  id: string;
  text: string;
  isDone: boolean;
}

export type AiChatAnswerState = "ANSWERING" | "DONE" | "STOPPED" | "FAILED";

export interface AiChatAnswer {
  kind: "answer";
  id: string;
  state: AiChatAnswerState;
  text: string;
  model: string;
  images: string[];
  activities: AiChatActivity[];
  steps: AiChatStepReferenceDto[];
  error: string;
  errorDetail: string;
  startedAt: number;
  firstTextAt?: number;
  endedAt?: number;
}

export type AiChatItem = AiChatQuestion | AiChatAnswer;

export interface AiConversation {
  id: string;
  title: string;
  createdAt: number;
  items: AiChatItem[];
  draft: string;

  /** The page whose chip was taken off. Another page brings the chip back. */
  droppedPageKey?: string;

  /** The executions whose images are off for the next question, because they went already or were switched off. */
  screenshotsOff: number[];
}

interface Props {
  isOpen: boolean;
  conversations: AiConversation[];
  activeId: string | undefined;

  /** Goes up whenever the input should take the focus. */
  focusTick: number;
}

interface Actions {
  open: () => void;
  close: () => void;
  toggle: () => void;
  focusInput: () => void;

  /** Keeps an empty conversation rather than leaving another one behind it. */
  startConversation: () => string;
  setActive: (id: string) => void;
  setDraft: (id: string, draft: string) => void;
  setDroppedPage: (id: string, pageKey: string | undefined) => void;
  setScreenshotsOn: (id: string, executionId: number, isOn: boolean) => void;

  addExchange: (id: string, text: string, context: AiChatContext, answerId: string) => void;
  restartAnswer: (id: string, answerId: string, newAnswerId: string) => void;
  applyEvent: (event: AiChatEventDto) => void;
}

const newConversation = (): AiConversation => ({
  id: crypto.randomUUID(),
  title: "New question",
  createdAt: Date.now(),
  items: [],
  draft: "",
  screenshotsOff: [],
});

const newAnswer = (id: string): AiChatAnswer => ({
  kind: "answer",
  id: id,
  state: "ANSWERING",
  text: "",
  model: "",
  images: [],
  activities: [],
  steps: [],
  error: "",
  errorDetail: "",
  startedAt: Date.now(),
});

/** An event this page decided on itself: it stopped the answer, or could not start it. */
export const localChatEvent = (
  answerId: string,
  type: AiChatEventTypeEnum,
  error = "",
  errorDetail = "",
): AiChatEventDto => ({
  answerId: answerId,
  type: type,
  model: "",
  images: [],
  activityId: "",
  activity: "",
  steps: [],
  text: "",
  error: error,
  errorDetail: errorDetail,
});

const endActivities = (activities: AiChatActivity[]): AiChatActivity[] =>
  activities.map((x) => ({ ...x, isDone: true }));

const withEvent = (answer: AiChatAnswer, event: AiChatEventDto): AiChatAnswer => {
  // A stopped answer's last words, still on their way, are not part of it.
  if (answer.state !== "ANSWERING") return answer;

  const now = Date.now();

  switch (event.type) {
    case AiChatEventTypeEnum.STARTED:
      return { ...answer, model: event.model, images: event.images };

    // What it wrote on the way to a tool was narration. The answer is what comes after the last one.
    case AiChatEventTypeEnum.TOOL_STARTED:
      return {
        ...answer,
        text: "",
        firstTextAt: undefined,
        activities: [...answer.activities, { id: event.activityId, text: event.activity, isDone: false }],
      };

    case AiChatEventTypeEnum.TOOL_FINISHED:
      return {
        ...answer,
        activities: answer.activities.map((x) => (x.id === event.activityId ? { ...x, isDone: true } : x)),
        steps: [...answer.steps, ...event.steps],
      };

    case AiChatEventTypeEnum.TEXT:
      return { ...answer, text: answer.text + event.text, firstTextAt: answer.firstTextAt ?? now };

    case AiChatEventTypeEnum.DONE:
      return {
        ...answer,
        state: "DONE",
        text: event.text,
        firstTextAt: answer.firstTextAt ?? now,
        endedAt: now,
        activities: endActivities(answer.activities),
      };

    case AiChatEventTypeEnum.STOPPED:
      return { ...answer, state: "STOPPED", endedAt: now, activities: endActivities(answer.activities) };

    case AiChatEventTypeEnum.FAILED:
      return {
        ...answer,
        state: "FAILED",
        text: "",
        error: event.error,
        errorDetail: event.errorDetail,
        endedAt: now,
        activities: endActivities(answer.activities),
      };

    default:
      return answer;
  }
};

const withoutId = (ids: number[], id: number): number[] => ids.filter((x) => x !== id);

export const useAiChatStore = create<Props & Actions>()(
  devtools((set, get) => ({
    isOpen: false,
    conversations: [],
    activeId: undefined,
    focusTick: 0,

    open: (): void =>
      set((state) => {
        if (state.conversations.length > 0) return { isOpen: true };

        const first = newConversation();
        return { isOpen: true, conversations: [first], activeId: first.id };
      }),

    close: (): void => set({ isOpen: false }),

    toggle: (): void => {
      if (get().isOpen) get().close();
      else get().open();
    },

    focusInput: (): void => set((state) => ({ focusTick: state.focusTick + 1 })),

    startConversation: (): string => {
      const active = get().conversations.find((x) => x.id === get().activeId);
      if (active && active.items.length === 0) return active.id;

      const next = newConversation();

      set((state) => ({
        conversations: [...state.conversations, next],
        activeId: next.id,
      }));

      return next.id;
    },

    setActive: (id: string): void => set({ activeId: id }),

    setDraft: (id: string, draft: string): void =>
      set((state) => ({
        conversations: state.conversations.map((x) => (x.id === id ? { ...x, draft: draft } : x)),
      })),

    setDroppedPage: (id: string, pageKey: string | undefined): void =>
      set((state) => ({
        conversations: state.conversations.map((x) => (x.id === id ? { ...x, droppedPageKey: pageKey } : x)),
      })),

    setScreenshotsOn: (id: string, executionId: number, isOn: boolean): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id !== id
            ? x
            : {
                ...x,
                screenshotsOff: isOn ? withoutId(x.screenshotsOff, executionId) : [...withoutId(x.screenshotsOff, executionId), executionId],
              },
        ),
      })),

    addExchange: (id: string, text: string, context: AiChatContext, answerId: string): void =>
      set((state) => ({
        conversations: state.conversations.map((x) => {
          if (x.id !== id) return x;

          // They go with one question. Another means another click.
          let screenshotsOff = x.screenshotsOff;
          if (context.executionId) screenshotsOff = [...withoutId(screenshotsOff, context.executionId), context.executionId];

          const question: AiChatQuestion = { kind: "question", id: crypto.randomUUID(), text: text, context: context };

          return {
            ...x,
            title: x.items.length === 0 ? text : x.title,
            items: [...x.items, question, newAnswer(answerId)],
            draft: "",
            screenshotsOff: screenshotsOff,
          };
        }),
      })),

    restartAnswer: (id: string, answerId: string, newAnswerId: string): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          x.id !== id
            ? x
            : { ...x, items: x.items.map((item) => (item.id === answerId ? newAnswer(newAnswerId) : item)) },
        ),
      })),

    applyEvent: (event: AiChatEventDto): void =>
      set((state) => ({
        conversations: state.conversations.map((x) =>
          !x.items.some((item) => item.id === event.answerId)
            ? x
            : {
                ...x,
                items: x.items.map((item) =>
                  item.kind === "answer" && item.id === event.answerId ? withEvent(item, event) : item,
                ),
              },
        ),
      })),
  })),
);
