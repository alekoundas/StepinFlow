import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import AiChatMarkdownComponent from "@/features/ai/components/AiChatMarkdownComponent";
import { useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { MUTED } from "@/features/execution/execution-format";
import type { AiChatAnswer } from "@/features/ai/store/ai-chat-store";

const COPIED_MILLISECONDS = 1500;

const secondsBetween = (from: number, to: number): number => Math.max(1, Math.round((to - from) / 1000));

function Thinking() {
  return (
    <div
      role="status"
      className="flex align-items-center gap-2 text-sm"
      style={MUTED}
    >
      <i
        className="pi pi-spin pi-spinner text-xs"
        style={{ color: "var(--primary-color)" }}
        aria-hidden="true"
      />
      <span className="status-pill-pulse">Thinking</span>
    </div>
  );
}

interface LookedAtRowsProps {
  answer: AiChatAnswer;
}

function LookedAtRows({ answer }: LookedAtRowsProps) {
  return (
    <div className="flex flex-column gap-2 text-sm">
      {answer.images.map((image) => (
        <div
          key={image}
          className="flex align-items-start gap-2"
          style={MUTED}
        >
          <i
            className="pi pi-image text-xs mt-1"
            aria-hidden="true"
          />
          <span style={{ overflowWrap: "anywhere" }}>{image}</span>
        </div>
      ))}

      {answer.activities.map((activity) => (
        <div
          key={activity.id}
          className="flex align-items-start gap-2"
          style={activity.isDone ? MUTED : undefined}
        >
          <i
            className={`pi text-xs mt-1 ${activity.isDone ? "pi-check" : "pi-spin pi-spinner"}`}
            style={activity.isDone ? undefined : { color: "var(--primary-color)" }}
            aria-hidden="true"
          />
          <span style={{ overflowWrap: "anywhere" }}>{activity.text}</span>
        </div>
      ))}
    </div>
  );
}

interface FailedCardProps {
  answer: AiChatAnswer;
  onTryAgain: () => void;
}

function FailedCard({ answer, onTryAgain }: FailedCardProps) {
  const navigate = useNavigate();

  return (
    <div
      role="alert"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={{
        borderColor: "color-mix(in srgb, var(--red-400) 35%, transparent)",
        background: "color-mix(in srgb, var(--red-400) 8%, transparent)",
      }}
    >
      <div className="flex gap-2">
        <i
          className="pi pi-exclamation-triangle mt-1"
          style={{ color: "var(--red-400)" }}
          aria-hidden="true"
        />
        <div className="flex flex-column gap-1 min-w-0">
          <span className="text-sm font-medium">{answer.error}</span>
          {answer.errorDetail ? (
            <span
              className="text-xs"
              style={{ ...MUTED, overflowWrap: "anywhere" }}
            >
              {answer.errorDetail}
            </span>
          ) : null}
        </div>
      </div>

      <div className="flex gap-2">
        <Button
          type="button"
          label="Try again"
          size="small"
          onClick={onTryAgain}
        />
        <Button
          type="button"
          label="AI settings"
          size="small"
          text
          severity="secondary"
          onClick={() => navigate("/settings/ai")}
        />
      </div>
    </div>
  );
}

interface Props {
  conversationId: string;
  answer: AiChatAnswer;

  /** Asking an earlier one again would leave what came after it answering a different thing. */
  isLast: boolean;
}

/** One answer: what it looked at as it happens, then the words, and what can be done with them. */
export default function AiChatAnswerComponent({ conversationId, answer, isLast }: Props) {
  const { askAgain } = useAskAi();

  const [isExpanded, setIsExpanded] = useState(false);
  const [isCopied, setIsCopied] = useState(false);

  const isAnswering = answer.state === "ANSWERING";
  const hasText = answer.text.length > 0;
  const lookedAtCount = answer.images.length + answer.activities.length;
  const hasEnded = answer.endedAt !== undefined;

  const isToolGoing = answer.activities.some((x) => !x.isDone);
  const isThinking = isAnswering && !hasText && !isToolGoing;

  const showRows = isAnswering && !hasText ? lookedAtCount > 0 : isExpanded;
  const showSummary = lookedAtCount > 0 && (hasText || hasEnded);

  let summary = `Looked at ${lookedAtCount} ${lookedAtCount === 1 ? "thing" : "things"}`;
  if (answer.firstTextAt !== undefined) summary += ` · ${secondsBetween(answer.startedAt, answer.firstTextAt)}s`;

  const copy = async () => {
    await navigator.clipboard.writeText(answer.text);

    setIsCopied(true);
    setTimeout(() => setIsCopied(false), COPIED_MILLISECONDS);
  };

  return (
    <article className="flex flex-column gap-2 min-w-0">
      {showSummary ? (
        <Button
          type="button"
          text
          severity="secondary"
          size="small"
          className="align-self-start gap-2 text-sm py-1 px-2"
          style={{ marginLeft: "-0.5rem" }}
          aria-expanded={isExpanded}
          onClick={() => setIsExpanded(!isExpanded)}
        >
          <i
            className="pi pi-check text-xs"
            aria-hidden="true"
          />
          <span>{summary}</span>
          <i
            className={`pi text-xs ${isExpanded ? "pi-chevron-down" : "pi-chevron-right"}`}
            aria-hidden="true"
          />
        </Button>
      ) : null}

      {showRows ? <LookedAtRows answer={answer} /> : null}

      {isThinking ? <Thinking /> : null}

      {/* The cursor sits inside the text so it lands wherever the last line ends. */}
      {hasText ? (
        <AiChatMarkdownComponent
          text={isAnswering ? `${answer.text} ▍` : answer.text}
          steps={answer.steps}
        />
      ) : null}

      {answer.state === "STOPPED" ? (
        <span
          className="text-sm"
          style={MUTED}
        >
          {hasText ? "You stopped this answer here." : "You stopped this before it answered."}
        </span>
      ) : null}

      {answer.state === "FAILED" ? (
        <FailedCard
          answer={answer}
          onTryAgain={() => askAgain(conversationId, answer.id)}
        />
      ) : null}

      {answer.state === "DONE" || answer.state === "STOPPED" ? (
        <div
          className="flex align-items-center gap-1"
          style={{ marginLeft: "-0.5rem" }}
        >
          {hasText ? (
            <Button
              type="button"
              icon={isCopied ? "pi pi-check" : "pi pi-copy"}
              text
              rounded
              severity="secondary"
              size="small"
              aria-label={isCopied ? "Copied" : "Copy answer"}
              onClick={copy}
            />
          ) : null}

          {isLast ? (
            <Button
              type="button"
              icon="pi pi-refresh"
              text
              rounded
              severity="secondary"
              size="small"
              aria-label="Ask again"
              onClick={() => askAgain(conversationId, answer.id)}
            />
          ) : null}

          {answer.model && answer.endedAt ? (
            <span
              className="text-xs ml-1"
              style={MUTED}
            >
              {answer.model} · {secondsBetween(answer.startedAt, answer.endedAt)}s
            </span>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}
