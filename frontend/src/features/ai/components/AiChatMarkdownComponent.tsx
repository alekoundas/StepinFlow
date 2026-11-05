import { createContext, useContext, useMemo } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import Markdown from "react-markdown";
import type { Components } from "react-markdown";

import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { MONO } from "@/features/execution/execution-format";
import type { AiChatStepReferenceDto } from "@/shared/models/ai-chat-dtos";

const StepsContext = createContext<Map<string, AiChatStepReferenceDto | null>>(new Map());

const keyOf = (name: string): string => name.trim().toLowerCase();

// Null under a name two different steps share: the link could only guess which one was meant.
const stepsByName = (steps: AiChatStepReferenceDto[]) => {
  const byName = new Map<string, AiChatStepReferenceDto | null>();

  for (const step of steps) {
    const key = keyOf(step.name);
    const seen = byName.get(key);

    if (seen === undefined) byName.set(key, step);
    else if (seen && seen.flowStepId !== step.flowStepId) byName.set(key, null);
  }

  return byName;
};

interface StepLinkProps {
  step: AiChatStepReferenceDto;
  children: ReactNode;
}

function StepLink({ step, children }: StepLinkProps) {
  const navigate = useNavigate();
  const setStepToReveal = useWorkflowStore((x) => x.setStepToReveal);

  return (
    <button
      type="button"
      title="Open this step"
      className="border-none border-round-md cursor-pointer font-medium text-left"
      style={{
        padding: "1px 6px",
        background: "var(--highlight-bg)",
        color: "var(--primary-color)",
        font: "inherit",
      }}
      onClick={() => {
        setStepToReveal(step.flowStepId);
        navigate(`/workflow/${step.flowId}`);
      }}
    >
      {children}
    </button>
  );
}

function Code({ children }: { children?: ReactNode }) {
  const steps = useContext(StepsContext);

  const text = String(children ?? "");
  const step = steps.get(keyOf(text));

  // A fenced block always ends in a line break, and its pre already frames it.
  if (text.includes("\n")) return <code style={{ fontFamily: MONO }}>{children}</code>;

  if (step) return <StepLink step={step}>{children}</StepLink>;

  return (
    <code
      className="border-round-md"
      style={{
        padding: "1px 5px",
        background: "color-mix(in srgb, var(--text-color) 9%, transparent)",
        fontFamily: MONO,
        fontSize: "0.88em",
      }}
    >
      {children}
    </code>
  );
}

// A heading in an answer is a line that stands out, not a page title, whatever its level.
function Heading({ children }: { children?: ReactNode }) {
  return <div className="font-semibold mb-2">{children}</div>;
}

const COMPONENTS: Components = {
  p: ({ children }) => <p style={{ margin: "0 0 0.6rem", lineHeight: 1.6 }}>{children}</p>,
  strong: ({ children }) => <strong className="font-semibold">{children}</strong>,
  ul: ({ children }) => <ul style={{ margin: "0 0 0.6rem", paddingLeft: "1.2rem" }}>{children}</ul>,
  ol: ({ children }) => <ol style={{ margin: "0 0 0.6rem", paddingLeft: "1.4rem" }}>{children}</ol>,
  li: ({ children }) => <li style={{ marginBottom: "0.25rem", lineHeight: 1.55 }}>{children}</li>,
  h1: Heading,
  h2: Heading,
  h3: Heading,
  h4: Heading,
  code: Code,
  pre: ({ children }) => (
    <pre
      className="border-round-lg overflow-x-auto"
      style={{
        background: "var(--surface-card)",
        padding: "0.6rem 0.8rem",
        margin: "0 0 0.6rem",
        fontSize: "0.85em",
      }}
    >
      {children}
    </pre>
  ),
};

interface Props {
  text: string;
  steps: AiChatStepReferenceDto[];
}

/**
 * An answer, as the model wrote it. A name in backticks that a tool returned as a step opens that
 * step; the model is never trusted to write a link.
 */
export default function AiChatMarkdownComponent({ text, steps }: Props) {
  const byName = useMemo(() => stepsByName(steps), [steps]);

  return (
    <StepsContext.Provider value={byName}>
      <div style={{ overflowWrap: "anywhere" }}>
        <Markdown components={COMPONENTS}>{text}</Markdown>
      </div>
    </StepsContext.Provider>
  );
}
