import { useRef, useSyncExternalStore } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { OverlayPanel } from "primereact/overlaypanel";

import AiChatComposerComponent from "@/features/ai/components/AiChatComposerComponent";
import AiChatThreadComponent from "@/features/ai/components/AiChatThreadComponent";
import { useAiChatAvailability, useAiChatImages, useAskAi } from "@/features/ai/hooks/use-ai-chat";
import { useAiChatPage } from "@/features/ai/hooks/use-ai-chat-page";
import type { AiChatPage } from "@/features/ai/hooks/use-ai-chat-page";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import type { AiChatContext, AiConversation } from "@/features/ai/store/ai-chat-store";
import { MUTED } from "@/features/execution/execution-format";
import { iconTile } from "@/features/home/home-format";

const WIDTH = 440;

// Narrower than this and pushing the page would leave too little of it, so the panel lies over it.
const NARROW_WINDOW = "(max-width: 1199px)";

const NO_PAGE: AiChatPage = { key: "none", kind: "none", label: "", didFail: false };

const SURFACE: CSSProperties = {
  width: WIDTH,
  background: "color-mix(in srgb, var(--surface-card) 55%, var(--surface-ground))",
  borderColor: "var(--surface-border)",
};

const OVERLAY: CSSProperties = {
  position: "fixed",
  top: 0,
  right: 0,
  zIndex: 1000,
  maxWidth: "100vw",
  boxShadow: "-12px 0 32px rgba(0, 0, 0, 0.45)",
};

const subscribeToNarrowWindow = (onChange: () => void) => {
  const query = window.matchMedia(NARROW_WINDOW);
  query.addEventListener("change", onChange);

  return () => query.removeEventListener("change", onChange);
};

const isWindowNarrow = () => window.matchMedia(NARROW_WINDOW).matches;

const isBusy = (conversation: AiConversation) =>
  conversation.items.some((x) => x.kind === "answer" && x.state === "ANSWERING");

const whenOf = (createdAt: number): string => {
  const minutes = Math.floor((Date.now() - createdAt) / 60_000);

  if (minutes < 1) return "Now";
  if (minutes < 60) return `${minutes} min ago`;

  const hours = Math.floor(minutes / 60);
  return hours === 1 ? "1 hour ago" : `${hours} hours ago`;
};

/**
 * Ask AI, docked beside the page rather than over it: the question is usually about the thing on
 * screen. It stays open from page to page, and what "this" means follows the page.
 */
export default function AiChatPanelComponent() {
  const isOpen = useAiChatStore((x) => x.isOpen);

  // Its own component, so nothing here asks the backend while the panel is closed.
  return isOpen ? <OpenPanel /> : null;
}

function OpenPanel() {
  const navigate = useNavigate();
  const isNarrow = useSyncExternalStore(subscribeToNarrowWindow, isWindowNarrow);
  const switcherRef = useRef<OverlayPanel>(null);

  const conversations = useAiChatStore((x) => x.conversations);
  const activeId = useAiChatStore((x) => x.activeId);
  const focusTick = useAiChatStore((x) => x.focusTick);
  const { close, startConversation, setActive, setDraft, setDroppedPage, setScreenshotsOn, focusInput } =
    useAiChatStore.getState();

  const { ask, stop } = useAskAi();
  const page = useAiChatPage();
  const { data: availability } = useAiChatAvailability(true);

  const conversation = conversations.find((x) => x.id === activeId);

  const isContextOn = conversation?.droppedPageKey !== page.key;
  const executionId = isContextOn ? page.executionId : undefined;

  const { data: images } = useAiChatImages(executionId, true);

  if (!conversation) return null;

  const areImagesOn = !!executionId && !conversation.screenshotsOff.includes(executionId);

  const contextOfQuestion = (): AiChatContext => {
    if (!isContextOn) return { sendScreenshots: false };

    return {
      flowId: page.flowId,
      executionId: page.executionId,
      sendScreenshots: areImagesOn && (images?.length ?? 0) > 0,
    };
  };

  const send = (question: string) => {
    if (isBusy(conversation)) return;

    ask(conversation.id, question.trim(), contextOfQuestion());
  };

  const stopAnswer = () => {
    const answering = conversation.items.find((x) => x.kind === "answer" && x.state === "ANSWERING");
    if (answering) stop(answering.id);
  };

  const startNew = () => {
    startConversation();
    switcherRef.current?.hide();
    focusInput();
  };

  return (
    <aside
      aria-label="Ask AI"
      className="flex flex-column flex-none h-screen border-left-1 text-sm"
      style={{ ...SURFACE, ...(isNarrow ? OVERLAY : undefined) }}
    >
      <header
        className="flex align-items-center gap-1 flex-none px-2 border-bottom-1"
        style={{ height: 52, borderColor: "var(--surface-border)" }}
      >
        <Button
          type="button"
          text
          severity="secondary"
          className="flex-1 min-w-0 gap-2 py-2 px-2"
          style={{ color: "var(--text-color)" }}
          aria-label="Switch conversation"
          onClick={(e) => switcherRef.current?.toggle(e)}
        >
          <i
            className="pi pi-sparkles"
            style={{ color: "var(--primary-color)" }}
            aria-hidden="true"
          />
          <span className="flex-1 min-w-0 text-left font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {conversation.title}
          </span>
          <i
            className="pi pi-chevron-down text-xs"
            style={MUTED}
            aria-hidden="true"
          />
        </Button>

        <Button
          type="button"
          icon="pi pi-plus"
          text
          rounded
          severity="secondary"
          aria-label="New question"
          tooltip="New question"
          tooltipOptions={{ position: "bottom" }}
          onClick={startNew}
        />
        <Button
          type="button"
          icon="pi pi-times"
          text
          rounded
          severity="secondary"
          aria-label="Close Ask AI"
          onClick={close}
        />

        <OverlayPanel
          ref={switcherRef}
          style={{ width: WIDTH - 24 }}
          pt={{ content: { className: "p-2" } }}
        >
          <div
            className="px-2 py-1 text-xs"
            style={MUTED}
          >
            Conversations
          </div>

          <div
            className="flex flex-column gap-1 overflow-y-auto"
            style={{ maxHeight: 320 }}
          >
            {[...conversations].reverse().map((x) => (
              <Button
                key={x.id}
                type="button"
                text
                severity="secondary"
                size="small"
                className="w-full gap-2 flex-none"
                style={{
                  color: "var(--text-color)",
                  background: x.id === conversation.id ? "var(--highlight-bg)" : undefined,
                }}
                onClick={() => {
                  setActive(x.id);
                  switcherRef.current?.hide();
                  focusInput();
                }}
              >
                <span className="flex-1 min-w-0 text-left white-space-nowrap overflow-hidden text-overflow-ellipsis">
                  {x.title}
                </span>
                {isBusy(x) ? (
                  <i
                    className="pi pi-spin pi-spinner text-xs flex-none"
                    style={{ color: "var(--primary-color)" }}
                    role="img"
                    aria-label="Answering"
                  />
                ) : (
                  <span
                    className="text-xs flex-none"
                    style={MUTED}
                  >
                    {whenOf(x.createdAt)}
                  </span>
                )}
              </Button>
            ))}
          </div>
        </OverlayPanel>
      </header>

      {availability && !availability.isAvailable ? (
        <div className="flex-1 min-h-0 flex flex-column align-items-center justify-content-center gap-3 p-5 text-center">
          <span
            className="flex align-items-center justify-content-center border-round-xl"
            style={{ width: 44, height: 44, ...iconTile("var(--yellow-400)") }}
          >
            <i
              className="pi pi-power-off text-lg"
              aria-hidden="true"
            />
          </span>
          <h2 className="m-0 text-lg font-semibold">Ask AI needs a model</h2>
          <p
            className="m-0 text-sm line-height-3"
            style={MUTED}
          >
            {availability.reason}
          </p>
          <Button
            type="button"
            label="Open AI settings"
            size="small"
            onClick={() => navigate("/settings/ai")}
          />
        </div>
      ) : (
        <>
          <AiChatThreadComponent
            key={conversation.id}
            conversation={conversation}
            page={isContextOn ? page : NO_PAGE}
            onAsk={send}
          />

          <AiChatComposerComponent
            draft={conversation.draft}
            page={page}
            isContextOn={isContextOn}
            images={images ?? []}
            areImagesOn={areImagesOn}
            availability={availability}
            isBusy={isBusy(conversation)}
            focusTick={focusTick}
            onDraft={(draft) => setDraft(conversation.id, draft)}
            onContextOn={(isOn) => setDroppedPage(conversation.id, isOn ? undefined : page.key)}
            onImagesOn={(isOn) => {
              if (executionId) setScreenshotsOn(conversation.id, executionId, isOn);
            }}
            onSend={() => send(conversation.draft)}
            onStop={stopAnswer}
          />
        </>
      )}
    </aside>
  );
}
