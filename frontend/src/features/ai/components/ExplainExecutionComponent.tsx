import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { useAiMutations, useAiStatus } from "@/features/ai/hooks/use-ai";
import type { AiAnswerDto } from "@/shared/models/ai-answer-dto";

interface Props {
  /** The run to explain. Undefined while nothing is open, which disables the button. */
  executionId: number | undefined;
}

/**
 * Asks the model to read a finished run and say what went wrong.
 *
 * The prompt is kept and can be shown, because the run is the user's own screen activity and they
 * should be able to see exactly what was sent before they trust a provider they pay for.
 */
export default function ExplainExecutionComponent({ executionId }: Props) {
  const { data: isConfigured } = useAiStatus();
  const { explainExecutionMutation } = useAiMutations();

  const [answer, setAnswer] = useState<AiAnswerDto | undefined>(undefined);
  const [isPromptShown, setPromptShown] = useState(false);

  const explain = async () => {
    if (!executionId) return;

    setPromptShown(false);
    setAnswer(await explainExecutionMutation.mutateAsync(executionId));
  };

  if (!isConfigured)
    return (
      <LabelComponent
        text="Set up an AI provider in Settings to have failures explained."
        size="sm"
        color="secondary"
      />
    );

  return (
    <div className="flex flex-column gap-3">
      <div className="flex align-items-center gap-2">
        <Button
          label="Explain this run"
          icon="pi pi-sparkles"
          size="small"
          outlined
          disabled={!executionId}
          loading={explainExecutionMutation.isPending}
          onClick={explain}
        />

        {answer?.prompt && (
          <Button
            label={isPromptShown ? "Hide what was sent" : "Show what was sent"}
            size="small"
            text
            onClick={() => setPromptShown(!isPromptShown)}
          />
        )}
      </div>

      {answer?.error && (
        <Message
          severity="warn"
          text={answer.error}
        />
      )}

      {answer?.answer && (
        <div className="surface-ground border-1 surface-border border-round p-3 white-space-pre-wrap">
          {answer.answer}
        </div>
      )}

      {isPromptShown && answer?.prompt && (
        <pre className="surface-ground border-1 surface-border border-round p-3 text-xs overflow-auto m-0">
          {answer.prompt}
        </pre>
      )}
    </div>
  );
}
