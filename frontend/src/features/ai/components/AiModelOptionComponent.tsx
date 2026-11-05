import LabelComponent from "@/shared/components/LabelComponent";
import type { AiModelDto } from "@/shared/models/ai-models-dto";

interface Props {
  model: AiModelDto;
}

/**
 * A model and its badges, as a row of a model dropdown.
 *
 * Which model to pick is otherwise a guess: tools decides whether it can answer questions about
 * your flows at all, and vision decides whether it can be shown a screenshot.
 */
export default function AiModelOptionComponent({ model }: Props) {
  const capabilities = model.capabilities ?? [];
  const shown = ["tools", "vision", "thinking"].filter((x) =>
    capabilities.includes(x),
  );

  return (
    <div className="flex align-items-center justify-content-between gap-2 w-full">
      <LabelComponent text={model.name} />

      <div className="flex align-items-center gap-1">
        {shown.map((capability) => (
          <CapabilityBadge
            key={capability}
            text={capability}
          />
        ))}

        {model.contextLength > 0 ? (
          <CapabilityBadge text={`${Math.round(model.contextLength / 1024)}K`} />
        ) : null}
      </div>
    </div>
  );
}

function CapabilityBadge({ text }: { text: string }) {
  return (
    <span
      style={{
        padding: "0.1rem 0.4rem",
        borderRadius: 4,
        border: "1px solid var(--surface-border)",
        background: "var(--surface-ground)",
        color: "var(--text-color-secondary)",
        fontSize: "0.65rem",
        whiteSpace: "nowrap",
      }}
    >
      {text}
    </span>
  );
}
