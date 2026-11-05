import { useLayoutEffect, useRef, useState } from "react";
import { Button } from "primereact/button";

import AiChatAnswerComponent from "@/features/ai/components/AiChatAnswerComponent";
import { MUTED } from "@/features/execution/execution-format";
import type { AiChatPage, AiChatPageKind } from "@/features/ai/hooks/use-ai-chat-page";
import type { AiConversation } from "@/features/ai/store/ai-chat-store";

// Scrolled this close to the end still counts as following the answer.
const FOLLOWING_WITHIN_PIXELS = 40;

const HINTS: Record<AiChatPageKind, string> = {
  execution: "Answers come from this execution, what it kept and your flows.",
  flow: "Answers come from this flow, its executions and the guide.",
  none: "Answers come from your flows, your executions and the guide.",
};

const suggestionsFor = (page: AiChatPage): string[] => {
  switch (page.kind) {
    case "execution":
      return [
        page.didFail ? "Why did this execution fail?" : "What happened in this execution?",
        "Has this flow always done this?",
        "What should I change?",
      ];
    case "flow":
      return ["What does this flow check?", "What does it mostly do?", "How reliable is it?"];
    default:
      return ["Which flows use Chrome?", "What failed most recently?", "How do I repeat a set of steps?"];
  }
};

interface Props {
  conversation: AiConversation;

  /** The page, or none when its chip was taken off. */
  page: AiChatPage;
  onAsk: (question: string) => void;
}

/** The conversation. It follows the answer as it grows, until the reader scrolls up to read. */
export default function AiChatThreadComponent({ conversation, page, onAsk }: Props) {
  const threadRef = useRef<HTMLDivElement>(null);
  const isFollowing = useRef(true);

  // How many items there were when the reader scrolled up. A new question ends it by itself.
  const [scrolledUpAt, setScrolledUpAt] = useState<number | undefined>(undefined);
  const isScrolledUp = scrolledUpAt === conversation.items.length;

  useLayoutEffect(() => {
    isFollowing.current = true;
  }, [conversation.items.length]);

  useLayoutEffect(() => {
    if (isFollowing.current && threadRef.current) threadRef.current.scrollTop = threadRef.current.scrollHeight;
  });

  const onScroll = () => {
    const thread = threadRef.current;
    if (!thread) return;

    isFollowing.current = thread.scrollHeight - thread.scrollTop - thread.clientHeight < FOLLOWING_WITHIN_PIXELS;
    setScrolledUpAt(isFollowing.current ? undefined : conversation.items.length);
  };

  const jumpToLatest = () => {
    isFollowing.current = true;
    setScrolledUpAt(undefined);

    if (threadRef.current) threadRef.current.scrollTop = threadRef.current.scrollHeight;
  };

  return (
    <div className="relative flex-1 min-h-0 flex">
      <div
        ref={threadRef}
        onScroll={onScroll}
        className="flex-1 min-w-0 overflow-y-auto flex flex-column gap-4 px-3 py-4"
      >
        {conversation.items.length === 0 ? (
          <div className="mt-auto flex flex-column gap-3 fadein animation-duration-200">
            <div className="flex flex-column gap-1">
              <h2 className="m-0 text-lg font-semibold">What do you want to know?</h2>
              <p
                className="m-0 text-sm"
                style={MUTED}
              >
                {HINTS[page.kind]}
              </p>
            </div>

            <div className="flex flex-column gap-2">
              {suggestionsFor(page).map((suggestion) => (
                <Button
                  key={suggestion}
                  type="button"
                  outlined
                  severity="secondary"
                  size="small"
                  className="w-full gap-2"
                  onClick={() => onAsk(suggestion)}
                >
                  <span className="flex-1 text-left">{suggestion}</span>
                  <i
                    className="pi pi-arrow-right text-xs"
                    aria-hidden="true"
                  />
                </Button>
              ))}
            </div>
          </div>
        ) : null}

        {conversation.items.map((item, index) =>
          item.kind === "question" ? (
            <div
              key={item.id}
              className="align-self-end text-sm"
              style={{
                maxWidth: "85%",
                padding: "9px 13px",
                borderRadius: "14px 14px 4px 14px",
                background: "color-mix(in srgb, var(--primary-color) 22%, var(--surface-card))",
                lineHeight: 1.5,
                whiteSpace: "pre-wrap",
                overflowWrap: "anywhere",
              }}
            >
              {item.text}
            </div>
          ) : (
            <AiChatAnswerComponent
              key={item.id}
              conversationId={conversation.id}
              answer={item}
              isLast={index === conversation.items.length - 1}
            />
          ),
        )}
      </div>

      {isScrolledUp ? (
        <Button
          type="button"
          label="Latest"
          icon="pi pi-arrow-down"
          size="small"
          rounded
          severity="secondary"
          className="absolute"
          style={{ left: "50%", bottom: 10, transform: "translateX(-50%)" }}
          onClick={jumpToLatest}
        />
      ) : null}
    </div>
  );
}
