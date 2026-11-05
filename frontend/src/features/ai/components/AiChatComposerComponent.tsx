import { useEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { InputTextarea } from "primereact/inputtextarea";

import { MONO, MUTED } from "@/features/execution/execution-format";
import type { AiChatPage, AiChatPageKind } from "@/features/ai/hooks/use-ai-chat-page";
import type { AiChatAvailabilityDto } from "@/shared/models/ai-chat-dtos";

const PLACEHOLDERS: Record<AiChatPageKind, string> = {
  execution: "Ask about this execution",
  flow: "Ask about this flow",
  none: "Ask about your flows",
};

const CHIP: CSSProperties = { height: 26, paddingInline: 8, font: "inherit" };

interface Props {
  draft: string;
  page: AiChatPage;
  isContextOn: boolean;
  images: string[];
  areImagesOn: boolean;
  availability: AiChatAvailabilityDto | undefined;
  isBusy: boolean;
  focusTick: number;
  onDraft: (draft: string) => void;
  onContextOn: (isOn: boolean) => void;
  onImagesOn: (isOn: boolean) => void;
  onSend: () => void;
  onStop: () => void;
}

/** What goes with the question, the question, and which model it is going to. */
export default function AiChatComposerComponent({
  draft,
  page,
  isContextOn,
  images,
  areImagesOn,
  availability,
  isBusy,
  focusTick,
  onDraft,
  onContextOn,
  onImagesOn,
  onSend,
  onStop,
}: Props) {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const [isFocused, setIsFocused] = useState(false);

  useEffect(() => {
    inputRef.current?.focus();
  }, [focusTick]);

  const hasPage = page.kind !== "none";
  const isOnExecution = page.kind === "execution" && isContextOn;
  const canSend = draft.trim().length > 0;

  const showImages = isOnExecution && images.length > 0;
  const showCloudNote = isOnExecution && !!availability?.isCloud && !availability.canSendScreenData;

  const placeholder = PLACEHOLDERS[isContextOn ? page.kind : "none"];

  let imagesStyle: CSSProperties = { borderColor: "var(--surface-border)", background: "transparent", ...MUTED };
  if (areImagesOn)
    imagesStyle = {
      borderColor: "color-mix(in srgb, var(--primary-color) 55%, transparent)",
      background: "var(--highlight-bg)",
      color: "var(--text-color)",
    };

  return (
    <div className="flex-none px-3 pb-3">
      <div
        className="flex flex-column gap-2 p-2 border-1 border-round-xl"
        style={{
          background: "var(--surface-card)",
          borderColor: isFocused ? "var(--primary-color)" : "var(--surface-border)",
          boxShadow: isFocused ? "0 0 0 3px var(--highlight-bg)" : undefined,
        }}
      >
        {hasPage ? (
          <div className="flex flex-wrap align-items-center gap-2 text-xs">
            {isContextOn ? (
              <span
                className="inline-flex align-items-center gap-2 border-round-lg min-w-0"
                style={{ ...CHIP, paddingRight: 3, background: "var(--highlight-bg)", maxWidth: "100%" }}
              >
                <i
                  className={`pi text-xs ${page.kind === "execution" ? "pi-wave-pulse" : "pi-sitemap"}`}
                  style={{ color: "var(--primary-color)" }}
                  aria-hidden="true"
                />
                <span className="min-w-0 white-space-nowrap overflow-hidden text-overflow-ellipsis">{page.label}</span>
                <button
                  type="button"
                  aria-label="Leave this out of the question"
                  className="flex-none flex align-items-center justify-content-center border-none cursor-pointer border-round-md"
                  style={{ width: 20, height: 20, padding: 0, background: "transparent", ...MUTED }}
                  onClick={() => onContextOn(false)}
                >
                  <i
                    className="pi pi-times"
                    style={{ fontSize: 10 }}
                    aria-hidden="true"
                  />
                </button>
              </span>
            ) : (
              <button
                type="button"
                className="inline-flex align-items-center gap-2 border-round-lg cursor-pointer min-w-0"
                style={{ ...CHIP, ...MUTED, background: "transparent", border: "1px dashed var(--surface-border)" }}
                onClick={() => onContextOn(true)}
              >
                <i
                  className="pi pi-plus"
                  style={{ fontSize: 10 }}
                  aria-hidden="true"
                />
                <span className="min-w-0 white-space-nowrap overflow-hidden text-overflow-ellipsis">{page.label}</span>
              </button>
            )}

            {showImages ? (
              <button
                type="button"
                aria-pressed={areImagesOn}
                title={`Show the model ${images.join(", ")}`}
                className="inline-flex align-items-center gap-2 border-1 border-round-lg cursor-pointer"
                style={{ ...CHIP, ...imagesStyle }}
                onClick={() => onImagesOn(!areImagesOn)}
              >
                <i
                  className="pi pi-image text-xs"
                  aria-hidden="true"
                />
                {images.length} {images.length === 1 ? "image" : "images"}
              </button>
            ) : null}
          </div>
        ) : null}

        {showCloudNote ? (
          <div
            className="text-xs line-height-3"
            style={MUTED}
          >
            The images stay on this computer. To send them, turn on "Let a cloud model see your screen" in AI settings.
          </div>
        ) : null}

        <InputTextarea
          ref={inputRef}
          value={draft}
          placeholder={placeholder}
          aria-label="Your question"
          rows={1}
          autoResize
          className="w-full border-none shadow-none p-1 text-sm"
          style={{ background: "transparent", maxHeight: 132, lineHeight: 1.5 }}
          onChange={(e) => onDraft(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          onKeyDown={(e) => {
            if (e.key !== "Enter" || e.shiftKey) return;

            e.preventDefault();
            if (canSend && !isBusy) onSend();
          }}
        />

        <div className="flex align-items-center justify-content-between gap-2">
          <Button
            type="button"
            text
            severity="secondary"
            size="small"
            className="min-w-0 gap-2 py-1 px-2 text-xs"
            tooltip="Open AI settings"
            tooltipOptions={{ position: "top" }}
            onClick={() => navigate("/settings/ai")}
          >
            {availability?.isCloud ? (
              <i
                className="pi pi-cloud text-xs"
                style={{ color: "var(--blue-400)" }}
                aria-hidden="true"
              />
            ) : (
              <span
                className="flex-none border-circle"
                style={{ width: 8, height: 8, background: "var(--green-400)" }}
              />
            )}
            <span
              className="white-space-nowrap overflow-hidden text-overflow-ellipsis"
              style={{ fontFamily: MONO, color: "var(--text-color)" }}
            >
              {availability?.model}
            </span>
            <span className="white-space-nowrap">{availability?.isCloud ? "in the cloud" : "on this machine"}</span>
          </Button>

          {isBusy ? (
            <Button
              type="button"
              icon="pi pi-stop"
              rounded
              severity="secondary"
              aria-label="Stop the answer"
              className="flex-none"
              style={{ width: 32, height: 32 }}
              onClick={onStop}
            />
          ) : (
            <Button
              type="button"
              icon="pi pi-arrow-up"
              rounded
              aria-label="Send"
              className="flex-none"
              style={{ width: 32, height: 32 }}
              disabled={!canSend}
              onClick={onSend}
            />
          )}
        </div>
      </div>
    </div>
  );
}
