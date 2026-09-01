import { Button } from "primereact/button";
import { ProgressBar } from "primereact/progressbar";

import LabelComponent from "@/shared/components/LabelComponent";
import { useAiModelSuggestions } from "@/features/ai/hooks/use-ai";
import { useAiModelPull } from "@/features/ai/hooks/use-ai-model-pull";
import type { AiModelSuggestionDto } from "@/shared/models/ai-model-suggestion-dto";
import type { AiModelPullEventDto } from "@/shared/models/ai-model-pull-event-dto";

interface Props {
  /** Only Ollama downloads anything. A paid provider hosts its own models. */
  isEnabled: boolean;
}

/**
 * Pulls a model onto this machine.
 *
 * Without this the model list is empty on a fresh Ollama and there is no way out of the settings
 * page - you would have to know to open a terminal. A download is gigabytes, so it reports as it
 * goes rather than spinning.
 */
export default function OllamaModelDownloadComponent({ isEnabled }: Props) {
  const { data: suggestions = [], isLoading } = useAiModelSuggestions(isEnabled);
  const { progress, isPulling, pull, dismiss } = useAiModelPull();

  if (!isEnabled) return null;

  return (
    <div className="flex flex-column gap-2 mt-2">
      <LabelComponent
        text="Models you can download"
        weight="semibold"
        size="sm"
      />

      {isLoading && (
        <LabelComponent
          text="Looking..."
          size="sm"
          color="secondary"
        />
      )}

      {suggestions.map((suggestion) => (
        <SuggestionRow
          key={suggestion.name}
          suggestion={suggestion}
          isPulling={isPulling}
          isPullingThis={isPulling && progress?.model === suggestion.name}
          onDownload={() => pull(suggestion.name)}
        />
      ))}

      {progress && (
        <div className="surface-ground border-1 surface-border border-round p-3 mt-2 flex flex-column gap-2">
          <div className="flex align-items-center justify-content-between gap-3">
            <LabelComponent
              text={statusText(progress)}
              size="sm"
              color={progress.error ? "error" : "secondary"}
            />

            {/* Only a failure needs closing. A finished download clears itself, and a running
                one cannot be dismissed - it would carry on regardless. */}
            {!!progress.error && (
              <Button
                label="Dismiss"
                size="small"
                text
                onClick={dismiss}
              />
            )}
          </div>

          {!progress.error && (
            <ProgressBar
              value={percentOf(progress.completed, progress.total)}
              // Ollama reports no total while it fetches the manifest, and a bar sitting at zero
              // reads as stuck rather than as starting.
              mode={progress.total > 0 ? "determinate" : "indeterminate"}
              style={{ height: "0.5rem" }}
            />
          )}
        </div>
      )}

      <LabelComponent
        text="Anything else in Ollama's library works too — pull it with Ollama and it will appear in the model list."
        size="xs"
        color="secondary"
      />
    </div>
  );
}

interface SuggestionRowProps {
  suggestion: AiModelSuggestionDto;
  isPulling: boolean;
  isPullingThis: boolean;
  onDownload: () => void;
}

function SuggestionRow({
  suggestion,
  isPulling,
  isPullingThis,
  onDownload,
}: SuggestionRowProps) {
  return (
    <div className="flex align-items-center justify-content-between gap-3">
      <div className="flex flex-column">
        <LabelComponent text={`${suggestion.name} · ${suggestion.size}`} />
        <LabelComponent
          text={suggestion.description}
          size="xs"
          color="secondary"
        />
      </div>

      {suggestion.isInstalled ? (
        <LabelComponent
          text="Downloaded"
          size="sm"
          color="success"
        />
      ) : (
        <Button
          label="Download"
          icon="pi pi-download"
          size="small"
          outlined
          // One at a time: two pulls at once compete for the same disk and the same progress bar.
          disabled={isPulling}
          loading={isPullingThis}
          onClick={onDownload}
        />
      )}
    </div>
  );
}

/** Ollama's own wording while it works, and something plainer at either end. */
function statusText(progress: AiModelPullEventDto): string {
  if (progress.error) return progress.error;

  if (progress.total > 0)
    return `${progress.model} — ${progress.status} · ${gigabytes(progress.completed)} of ${gigabytes(progress.total)}`;

  return `${progress.model} — ${progress.status}`;
}

function gigabytes(bytes: number): string {
  return `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`;
}

function percentOf(completed: number, total: number): number {
  if (total <= 0) return 0;
  return Math.round((completed / total) * 100);
}
