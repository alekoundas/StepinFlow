import type { CSSProperties, ReactNode } from "react";
import { Navigate, NavLink, useParams } from "react-router-dom";

import OcrLanguagesPanelComponent from "@/features/settings/components/OcrLanguagesPanelComponent";
import AppSettingsPanelComponent from "@/features/settings/components/AppSettingsPanelComponent";
import DiscordBotsPanelComponent from "@/features/settings/components/DiscordBotsPanelComponent";
import HotkeysPanelComponent from "@/features/settings/components/HotkeysPanelComponent";
import AiSettingsPanelComponent from "@/features/settings/components/AiSettingsPanelComponent";
import RepositoryPanelComponent from "@/features/settings/components/RepositoryPanelComponent";

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
        description="What a run keeps behind it."
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
      <HotkeysPanelComponent
        header="Recording keys"
        description="Pressed while you are recording another application, so these are matched system wide and never recorded. They still reach that application, so rebind any it uses."
        keyPrefix="RECORDING_HOTKEY_"
      />
    ),
  },
  {
    path: "debugger-keys",
    label: "Debugger keys",
    icon: "step-forward",
    content: (
      <HotkeysPanelComponent
        header="Debugger keys"
        description="Pressed while you are looking at the application being automated, not at StepinFlow, so these are matched system wide. That also means they still do whatever they normally do in that application - rebind any that clash with what a flow drives."
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
    <div
      className="m-4 flex flex-column gap-4"
      style={{ maxWidth: 1240 }}
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
        <nav
          aria-label="Settings sections"
          className="flex flex-column flex-none gap-1"
          style={{ width: 220, position: "sticky", top: 16 }}
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

        {/* Keyed, so two sections built from the same panel never share its state. */}
        <div
          key={section.path}
          className="flex-1 min-w-0"
        >
          {section.content}
        </div>
      </div>
    </div>
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
