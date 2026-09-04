import { useEffect, useRef, useState } from "react";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import { InputTextarea } from "primereact/inputtextarea";
import { Message } from "primereact/message";
import { classNames } from "primereact/utils";
import Markdown from "react-markdown";

import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import { useAiChatAvailability, useAskAi } from "@/features/ai/hooks/use-ai-chat";
import type { AiConversation } from "@/features/ai/store/ai-chat-store";

/**
 * Asking about your own flows, in a window that hovers over whatever you were doing - the question
 * is usually about the thing on screen, so it does not take you away from it.
 *
 * Tabs because one question leads to another about something else, and losing the first to ask the
 * second is the annoying part of a single thread.
 */
export default function AiChatDialogComponent() {
  const {
    isOpen,
    conversations,
    activeId,
    close,
    startConversation,
    closeConversation,
    setActive,
  } = useAiChatStore();

  const { data: availability } = useAiChatAvailability(isOpen);
  const { ask } = useAskAi();

  const [question, setQuestion] = useState("");

  const active = conversations.find((x) => x.id === activeId);

  const send = () => {
    if (!active || !question.trim() || active.isPending) return;

    ask(active.id, question.trim());
    setQuestion("");
  };

  return (
    <Dialog
      visible={isOpen}
      onHide={close}
      header="Ask about your flows"
      draggable
      resizable
      maximizable
      // Not modal: the point is to read the app while asking about it.
      modal={false}
      style={{ width: "44rem", height: "36rem" }}
      contentClassName="flex flex-column p-0"
    >
      <TabStrip
        conversations={conversations}
        activeId={activeId}
        onSelect={setActive}
        onClose={closeConversation}
        onNew={startConversation}
      />

      {availability && !availability.isAvailable ? (
        <div className="p-3">
          <Message
            severity="warn"
            text={availability.reason}
          />
        </div>
      ) : (
        <>
          <MessageList conversation={active} />

          <div className="flex gap-2 p-3 border-top-1 surface-border">
            <InputTextarea
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              onKeyDown={(e) => {
                // Enter sends, shift+enter breaks the line - what every chat does.
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  send();
                }
              }}
              placeholder="Which flows use Chrome?"
              autoResize
              rows={1}
              className="flex-1"
            />

            <Button
              icon="pi pi-send"
              onClick={send}
              disabled={!question.trim() || !!active?.isPending}
              loading={!!active?.isPending}
            />
          </div>
        </>
      )}
    </Dialog>
  );
}

interface TabStripProps {
  conversations: AiConversation[];
  activeId: string | undefined;
  onSelect: (id: string) => void;
  onClose: (id: string) => void;
  onNew: () => void;
}

function TabStrip({
  conversations,
  activeId,
  onSelect,
  onClose,
  onNew,
}: TabStripProps) {
  return (
    <div className="flex align-items-center gap-1 px-2 pt-2 border-bottom-1 surface-border overflow-x-auto">
      {conversations.map((conversation) => (
        <div
          key={conversation.id}
          onClick={() => onSelect(conversation.id)}
          className={classNames(
            "flex align-items-center gap-2 px-3 py-2 border-round-top cursor-pointer white-space-nowrap",
            conversation.id === activeId ? "surface-card" : "surface-ground",
          )}
        >
          <LabelComponent
            text={conversation.title}
            size="sm"
            color={conversation.id === activeId ? undefined : "secondary"}
            wrap={false}
          />

          <i
            className="pi pi-times text-xs"
            onClick={(e) => {
              e.stopPropagation();
              onClose(conversation.id);
            }}
          />
        </div>
      ))}

      <Button
        icon="pi pi-plus"
        text
        size="small"
        onClick={onNew}
      />
    </div>
  );
}

interface MessageListProps {
  conversation: AiConversation | undefined;
}

function MessageList({ conversation }: MessageListProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [conversation?.messages.length, conversation?.isPending]);

  if (!conversation) return null;

  return (
    <div className="flex-1 overflow-y-auto flex flex-column gap-3 p-3">
      {conversation.messages.length === 0 ? (
        <div className="flex flex-column align-items-center gap-2 p-5 text-center">
          <IconComponent
            name="sparkles"
            size="lg"
            className="text-color-secondary opacity-50"
          />
          <LabelComponent
            text="Ask about your flows, their steps, past runs or settings."
            color="secondary"
            size="sm"
          />
          <LabelComponent
            text="Answers come from your database, not from memory."
            color="secondary"
            size="xs"
          />
        </div>
      ) : null}

      {conversation.messages.map((message, index) => (
        <div
          key={index}
          className={classNames("flex", {
            "justify-content-end": message.role === "user",
          })}
        >
          <div
            className={classNames(
              "border-round p-3",
              message.role === "user"
                ? "surface-100 text-900 white-space-pre-wrap"
                : "surface-ground border-1 surface-border",
            )}
            style={{ maxWidth: "85%" }}
          >
            {message.role === "user" ? (
              message.text
            ) : (
              <AnswerComponent text={message.text} />
            )}
          </div>
        </div>
      ))}

      {/* What it actually read, so an answer can be checked rather than taken on trust. The
          pictures are shown apart from the tools because it chose the tools and was simply handed
          these - and an answer that says nothing about a screenshot it was given reads differently
          once you can see it had one. */}
      {(conversation.toolCalls.length > 0 || conversation.images.length > 0) &&
      !conversation.isPending ? (
        <div className="flex flex-wrap gap-1 align-items-center">
          <LabelComponent
            text="Looked at:"
            size="xs"
            color="secondary"
          />
          {conversation.toolCalls.map((toolCall, index) => (
            <span
              key={`${toolCall}-${index}`}
              className="p-tag text-xs"
            >
              {toolCall}
            </span>
          ))}
          {conversation.images.map((image, index) => (
            <span
              key={`${image}-${index}`}
              className="p-tag text-xs flex align-items-center gap-1"
              style={{
                background: "transparent",
                color: "var(--text-color-secondary)",
                border: "1px solid var(--surface-border)",
              }}
            >
              <IconComponent
                name="image"
                className="text-xs"
              />
              {image}
            </span>
          ))}
        </div>
      ) : null}

      {conversation.isPending ? (
        <LabelComponent
          text="Reading your flows..."
          size="sm"
          color="secondary"
        />
      ) : null}

      {conversation.error ? (
        <Message
          severity="warn"
          text={conversation.error}
        />
      ) : null}

      <div ref={bottomRef} />
    </div>
  );
}

interface AnswerComponentProps {
  text: string;
}

/**
 * An answer, as the model wrote it.
 *
 * The model is told to write markdown, so a bold lead, a list of options and a step name in
 * backticks all arrive as markup. Rendered as plain text they read as one wall with asterisks in
 * it, which is how a good answer ends up looking like a bad one.
 *
 * Every element is styled inline rather than by a stylesheet, and sized down: a heading in a chat
 * bubble is a line that stands out, not a page title.
 */
function AnswerComponent({ text }: AnswerComponentProps) {
  return (
    <Markdown
      components={{
        p: ({ children }) => (
          <p style={{ margin: "0 0 0.6rem", lineHeight: 1.55 }}>{children}</p>
        ),
        strong: ({ children }) => (
          <strong style={{ fontWeight: 600, color: "var(--text-color)" }}>{children}</strong>
        ),
        ul: ({ children }) => (
          <ul style={{ margin: "0 0 0.6rem", paddingLeft: "1.1rem" }}>{children}</ul>
        ),
        ol: ({ children }) => (
          <ol style={{ margin: "0 0 0.6rem", paddingLeft: "1.3rem" }}>{children}</ol>
        ),
        li: ({ children }) => (
          <li style={{ marginBottom: "0.25rem", lineHeight: 1.5 }}>{children}</li>
        ),
        h1: ({ children }) => <ChatHeading>{children}</ChatHeading>,
        h2: ({ children }) => <ChatHeading>{children}</ChatHeading>,
        h3: ({ children }) => <ChatHeading>{children}</ChatHeading>,
        code: ({ children }) => (
          <code
            style={{
              background: "var(--surface-card)",
              border: "1px solid var(--surface-border)",
              borderRadius: 4,
              padding: "0.05rem 0.3rem",
              fontSize: "0.85em",
            }}
          >
            {children}
          </code>
        ),
        pre: ({ children }) => (
          <pre
            style={{
              background: "var(--surface-card)",
              border: "1px solid var(--surface-border)",
              borderRadius: 6,
              padding: "0.6rem 0.8rem",
              margin: "0 0 0.6rem",
              overflowX: "auto",
              fontSize: "0.85em",
            }}
          >
            {children}
          </pre>
        ),
      }}
    >
      {text}
    </Markdown>
  );
}

interface ChatHeadingProps {
  children: React.ReactNode;
}

// Every heading level renders the same: inside a bubble the depth means nothing, and a real h1
// would be larger than the dialog title above it.
function ChatHeading({ children }: ChatHeadingProps) {
  return (
    <div
      style={{
        fontWeight: 600,
        fontSize: "0.95rem",
        margin: "0.3rem 0 0.4rem",
        color: "var(--text-color)",
      }}
    >
      {children}
    </div>
  );
}
