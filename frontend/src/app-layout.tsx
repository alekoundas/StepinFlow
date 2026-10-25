import type { CSSProperties, ReactNode } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { ScrollPanel } from "primereact/scrollpanel";
import { Tooltip } from "primereact/tooltip";

import AiChatDialogComponent from "@/features/ai/components/AiChatDialogComponent";
import { useAiChatStore } from "@/features/ai/store/ai-chat-store";
import { useAiStatus } from "@/features/ai/hooks/use-ai";
import { useRepositoryEvents } from "@/features/repository/hooks/use-repository";
import { useAppSettings } from "@/features/settings/hooks/use-app-settings";
import { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";
import { AiProviderEnum } from "@/shared/enums/backend/ai/ai-provider-enum";

// Pages that want the width: the rail keeps only its icons there.
const NARROW_PATHS = ["/workflow/", "/execution/"];

const RAIL: CSSProperties = {
  background: "color-mix(in srgb, var(--surface-card) 55%, var(--surface-ground))",
  borderColor: "var(--surface-border)",
};

/**
 * The app around every page: the rail on the left and the page beside it. Home, then the flows,
 * sub-flows and executions you work with; Import and Ask AI sit apart, Settings at the bottom with
 * the model the AI features use always in view.
 */
export default function AppLayout() {
  const { pathname } = useLocation();
  const openAiChat = useAiChatStore((state) => state.open);
  useRepositoryEvents();

  const isNarrow = NARROW_PATHS.some((x) => pathname.startsWith(x));

  return (
    <div className="flex h-screen">
      <nav
        aria-label="Main"
        className="flex flex-column flex-none gap-1 py-3 border-right-1 h-screen"
        style={{ ...RAIL, width: isNarrow ? 64 : 232, paddingInline: isNarrow ? 10 : 12 }}
      >
        <Tooltip
          target=".nav-rail-item"
          position="right"
          disabled={!isNarrow}
        />

        <LogoComponent isNarrow={isNarrow} />

        <NavItemComponent
          to="/"
          icon="home"
          label="Home"
          isNarrow={isNarrow}
        />
        <NavItemComponent
          to="/flows"
          icon="sitemap"
          label="Flows"
          isNarrow={isNarrow}
        />
        <NavItemComponent
          to="/sub-flows"
          icon="clone"
          label="Sub-flows"
          isNarrow={isNarrow}
        />
        <NavItemComponent
          to="/executions"
          icon="wave-pulse"
          label="Executions"
          isNarrow={isNarrow}
        />

        <div
          className="my-2 mx-2"
          style={{ height: 1, background: "var(--surface-border)" }}
        />

        <NavItemComponent
          to="/import"
          icon="file-import"
          label="Import"
          isNarrow={isNarrow}
        />

        <button
          type="button"
          onClick={openAiChat}
          className="nav-rail-item flex align-items-center gap-2 border-none border-round-lg cursor-pointer"
          style={{ ...itemStyle(false, isNarrow), font: "inherit" }}
          aria-label={isNarrow ? "Ask AI" : undefined}
          data-pr-tooltip="Ask AI"
        >
          <i
            className="pi pi-sparkles"
            aria-hidden="true"
          />
          {!isNarrow && "Ask AI"}
        </button>

        <div className="flex-grow-1" />

        <NavItemComponent
          to="/settings"
          icon="sliders-h"
          label="Settings"
          isNarrow={isNarrow}
        />
        <AiModelComponent isNarrow={isNarrow} />
      </nav>

      <main className="flex-1 min-w-0 h-screen">
        <ScrollPanel className="h-full">
          <Outlet />
        </ScrollPanel>
      </main>

      <AiChatDialogComponent />
    </div>
  );
}

function LogoComponent({ isNarrow }: { isNarrow: boolean }) {
  return (
    <div
      className="flex align-items-center gap-2 pb-3 px-2"
      style={{ justifyContent: isNarrow ? "center" : undefined }}
    >
      <svg
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="none"
        aria-hidden="true"
      >
        <rect
          x="3"
          y="3"
          width="8"
          height="8"
          rx="2"
          fill="var(--primary-color)"
        />
        <rect
          x="13"
          y="13"
          width="8"
          height="8"
          rx="2"
          stroke="var(--primary-color)"
          strokeWidth="2"
        />
        <path
          d="M7 11v4a2 2 0 0 0 2 2h4"
          stroke="var(--primary-color)"
          strokeWidth="2"
        />
      </svg>
      {!isNarrow && <span className="font-semibold text-lg">StepinFlow</span>}
    </div>
  );
}

function NavItemComponent({ to, icon, label, isNarrow }: { to: string; icon: string; label: string; isNarrow: boolean }) {
  return (
    // Home is only home at "/" itself; every other path is under it.
    <NavLink
      to={to}
      end={to === "/"}
      className="nav-rail-item flex align-items-center gap-2 border-round-lg no-underline"
      style={({ isActive }) => itemStyle(isActive, isNarrow)}
      aria-label={isNarrow ? label : undefined}
      data-pr-tooltip={label}
    >
      <i
        className={`pi pi-${icon}`}
        aria-hidden="true"
      />
      {!isNarrow && label}
    </NavLink>
  );
}

// The model every AI feature uses, and where it runs; Settings is where it changes.
function AiModelComponent({ isNarrow }: { isNarrow: boolean }) {
  const { data: settings } = useAppSettings();
  const { data: isConfigured } = useAiStatus();

  const provider = settings?.find((x) => x.key === AppSettingKeyEnum.AI_PROVIDER)?.value ?? AiProviderEnum.NONE;
  const model = settings?.find((x) => x.key === AppSettingKeyEnum.AI_MODEL)?.value ?? "";

  let name = "No AI";
  let where = "Choose a model in Settings";
  let dot = "var(--surface-400)";

  if (provider === AiProviderEnum.OLLAMA) where = "Ollama · on this machine";
  if (provider === AiProviderEnum.OPENAI) where = "OpenAI · in the cloud";
  if (provider !== AiProviderEnum.NONE) name = model || "No model chosen";
  if (isConfigured) dot = "var(--green-400)";

  return (
    <NavLink
      to="/settings"
      className="nav-rail-item flex align-items-center gap-2 mt-2 p-2 border-1 border-round-lg no-underline"
      style={{
        borderColor: "var(--surface-border)",
        background: "var(--surface-card)",
        justifyContent: isNarrow ? "center" : undefined,
      }}
      aria-label={`${name}, ${where}`}
      data-pr-tooltip={`${name} · ${where}`}
    >
      <span
        className="flex-none border-circle"
        style={{ width: 8, height: 8, background: dot }}
      />
      {!isNarrow && (
        <span className="flex flex-column min-w-0">
          <TruncatedComponent style={{ color: "var(--text-color)" }}>{name}</TruncatedComponent>
          <TruncatedComponent style={{ color: "var(--text-color-secondary)" }}>{where}</TruncatedComponent>
        </span>
      )}
    </NavLink>
  );
}

function TruncatedComponent({ style, children }: { style: CSSProperties; children: ReactNode }) {
  return (
    <span
      className="text-xs white-space-nowrap overflow-hidden text-overflow-ellipsis"
      style={style}
    >
      {children}
    </span>
  );
}

function itemStyle(isActive: boolean, isNarrow: boolean): CSSProperties {
  let style: CSSProperties = {
    padding: "9px 10px",
    background: "transparent",
    color: "var(--text-color-secondary)",
  };

  if (isNarrow) style = { ...style, width: 44, height: 44, padding: 0, justifyContent: "center" };
  if (isActive)
    style = {
      ...style,
      background: "color-mix(in srgb, var(--text-color) 9%, transparent)",
      color: "var(--text-color)",
      fontWeight: 500,
    };

  return style;
}
