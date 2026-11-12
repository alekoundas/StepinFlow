import type { CSSProperties, ReactNode } from "react";
import { Navigate, NavLink, useParams } from "react-router-dom";

import OcrLanguagesPanelComponent from "@/features/settings/components/OcrLanguagesPanelComponent";
import AppSettingsPanelComponent from "@/features/settings/components/AppSettingsPanelComponent";
import DiscordBotsPanelComponent from "@/features/settings/components/DiscordBotsPanelComponent";
import HotkeysPanelComponent from "@/features/settings/components/HotkeysPanelComponent";
import AiSettingsPanelComponent from "@/features/settings/components/AiSettingsPanelComponent";
import RepositoryPanelComponent from "@/features/settings/components/RepositoryPanelComponent";
import AppVersionComponent from "@/features/auto-updater/components/AppVersionComponent";
import PageComponent from "@/shared/components/PageComponent";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Section {
  path: string;
  label: string;
  icon: string;
  content: ReactNode;
}

const SECTIONS: Section[] = [
  {
    path: "execution",
    label: "Execution",
    icon: "play",
    content: (
      <AppSettingsPanelComponent
        header="Execution"
        description="What an execution saves, so you can look back at what it did."
        keyPrefix="EXECUTION_"
      />
    ),
  },
  {
    path: "repository",
    label: "Repository",
    icon: "history",
    content: <RepositoryPanelComponent />,
  },
  {
    path: "ai",
    label: "AI",
    icon: "sparkles",
    content: <AiSettingsPanelComponent />,
  },
  {
    path: "text-recognition",
    label: "Text recognition",
    icon: "language",
    content: <OcrLanguagesPanelComponent />,
  },
  {
    path: "recording-keys",
    label: "Recording keys",
    icon: "video",
    content: (
      <div className="flex flex-column gap-3">
        <HotkeysPanelComponent
          header="Recording keys"
          description="These work while you record, whichever window is in front, and are never recorded. The application still gets the keys, so pick ones it does not use; the two clicks never reach it."
          keyPrefix="RECORDING_HOTKEY_"
        />
        <AppSettingsPanelComponent
          header="When you mark a check or a step"
          description="What happens when you mark a check or a step while recording."
          keyPrefix="RECORDING_GESTURE_"
        />
      </div>
    ),
  },
  {
    path: "debugger-keys",
    label: "Debugger keys",
    icon: "step-forward",
    content: (
      <HotkeysPanelComponent
        header="Debugger keys"
        description="These work while a flow executes, whichever window is in front, so you can pause or step through it without switching back to StepinFlow. The window in front still gets the key too, so pick keys the application does not use."
        keyPrefix="HOTKEY_"
      />
    ),
  },
  {
    path: "discord-bots",
    label: "Discord bots",
    icon: "discord",
    content: <DiscordBotsPanelComponent />,
  },
];

/** Every settings card, one at a time: the section list on the left, the one picked beside it. */
export default function SettingsPage() {
  const { section: path } = useParams();
  const section = SECTIONS.find((x) => x.path === path);

  if (!section)
    return (
      <Navigate
        to={`/settings/${SECTIONS[0].path}`}
        replace
      />
    );

  return (
    <PageComponent
      width="form"
      className="flex flex-column gap-4"
    >
      <div className="flex flex-column gap-1">
        <h1
          className="m-0 text-3xl font-semibold"
          style={{ letterSpacing: "-0.02em" }}
        >
          Settings
        </h1>
        <p
          className="m-0"
          style={MUTED}
        >
          How the app records, executes and keeps your flows.
        </p>
      </div>

      <div className="flex align-items-start gap-5">
        <div
          className="flex-none"
          style={{ width: 220, position: "sticky", top: 16 }}
        >
          <nav
            aria-label="Settings sections"
            className="flex flex-column gap-1"
          >
            {SECTIONS.map((x) => (
              <NavLink
                key={x.path}
                to={`/settings/${x.path}`}
                className="flex align-items-center gap-2 border-round-lg no-underline"
                style={({ isActive }) => itemStyle(isActive)}
              >
                <i
                  className={`pi pi-${x.icon}`}
                  aria-hidden="true"
                />
                {x.label}
              </NavLink>
            ))}
          </nav>

          <AppVersionComponent />
        </div>

        {/* Keyed, so two sections built from the same panel never share its state. */}
        <div
          key={section.path}
          className="flex-1 min-w-0"
        >
          {section.content}
        </div>
      </div>
    </PageComponent>
  );
}

function itemStyle(isActive: boolean): CSSProperties {
  let style: CSSProperties = {
    padding: "9px 12px",
    background: "transparent",
    color: "var(--text-color-secondary)",
  };

  if (isActive)
    style = {
      ...style,
      background: "color-mix(in srgb, var(--text-color) 9%, transparent)",
      color: "var(--text-color)",
      fontWeight: 500,
    };

  return style;
}
