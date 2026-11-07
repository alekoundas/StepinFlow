import { useState, type CSSProperties, type KeyboardEvent } from "react";
import { InputText } from "primereact/inputtext";

import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import {
  FLOW_STEP_CATALOG,
  FlowStepGroupEnum,
  flowStepAccent,
  hasBranches,
  tint,
  type FlowStepCatalogEntry,
} from "@/shared/models/flow-step-catalog";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

const ENTRIES: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fill, minmax(250px, 1fr))",
  gap: 8,
};

const RECENT_KEY = "step-picker-recent";
const RECENT_COUNT = 3;

const SECTIONS: { group: FlowStepGroupEnum; title: string; subtitle: string }[] = [
  {
    group: FlowStepGroupEnum.PERCEPTION,
    title: "Look at the screen",
    subtitle: "What is on screen decides what happens next",
  },
  { group: FlowStepGroupEnum.INPUT, title: "Mouse and keyboard", subtitle: "Act where the cursor or the focus is" },
  {
    group: FlowStepGroupEnum.CONTROL,
    title: "Flow control",
    subtitle: "Timing, repeating and the shape of the flow",
  },
  {
    group: FlowStepGroupEnum.WINDOW,
    title: "Windows",
    subtitle: "Each finds its window through a window area",
  },
  { group: FlowStepGroupEnum.SYSTEM, title: "System", subtitle: "This machine and the outside world" },
];

// Success and Failure come with the step that has them.
const PICKABLE = FLOW_STEP_CATALOG.filter((x) => x.group !== FlowStepGroupEnum.BRANCH);

const isMatch = (entry: FlowStepCatalogEntry, words: string[]): boolean => {
  const text = `${entry.label} ${entry.description} ${entry.searchWords}`.toLowerCase();
  return words.every((word) => text.includes(word));
};

/** What Enter adds: a label that starts with what was typed, then one with a word that does. */
const bestOf = (entries: FlowStepCatalogEntry[], typed: string): FlowStepCatalogEntry | undefined =>
  entries.find((x) => x.label.toLowerCase().startsWith(typed)) ??
  entries.find((x) =>
    x.label
      .toLowerCase()
      .split(" ")
      .some((word) => word.startsWith(typed)),
  ) ??
  entries[0];

const elementIdOf = (type: FlowStepTypeEnum): string => `step-picker-${type}`;

const readRecent = (): FlowStepTypeEnum[] => {
  try {
    const saved: unknown = JSON.parse(localStorage.getItem(RECENT_KEY) ?? "[]");
    if (!Array.isArray(saved)) return [];

    return saved.filter((x): x is FlowStepTypeEnum => PICKABLE.some((entry) => entry.flowStepType === x));
  } catch {
    return [];
  }
};

const remember = (type: FlowStepTypeEnum): void => {
  const recent = [type, ...readRecent().filter((x) => x !== type)].slice(0, RECENT_COUNT);
  localStorage.setItem(RECENT_KEY, JSON.stringify(recent));
};

/**
 * What the + in the tree opens: every step type, by section and in the tree's colours, under a
 * search that has the focus. Enter adds the one marked, which is the best match for what was typed;
 * the arrows move the mark. Recent is the last types picked on this machine.
 */
export default function FlowStepPickerComponent() {
  const setSelectedFlowStepTypeToAdd = useWorkflowStore((x) => x.setSelectedFlowStepTypeToAdd);

  const [query, setQuery] = useState("");
  const [markedType, setMarkedType] = useState<FlowStepTypeEnum>();
  const [recent] = useState(readRecent);

  const typed = query.trim().toLowerCase();
  const words = typed.split(/\s+/).filter((x) => x.length > 0);

  // In the order the sections show them, which is the order the arrows go through.
  const shown = SECTIONS.flatMap((section) =>
    PICKABLE.filter((x) => x.group === section.group && isMatch(x, words)),
  );
  const marked = shown.find((x) => x.flowStepType === markedType) ?? bestOf(shown, typed);

  const pick = (flowStepType: FlowStepTypeEnum) => {
    remember(flowStepType);
    setSelectedFlowStepTypeToAdd(flowStepType);
  };

  const search = (text: string) => {
    setQuery(text);
    setMarkedType(undefined);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape" && query) {
      event.preventDefault();
      search("");
      return;
    }

    if (!marked) return;

    if (event.key === "Enter") {
      event.preventDefault();
      pick(marked.flowStepType);
      return;
    }

    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;

    event.preventDefault();
    const step = event.key === "ArrowDown" ? 1 : -1;
    const next = shown[(shown.indexOf(marked) + step + shown.length) % shown.length];
    setMarkedType(next.flowStepType);
    document.getElementById(elementIdOf(next.flowStepType))?.scrollIntoView({ block: "nearest" });
  };

  const recentEntries = recent
    .map((x) => PICKABLE.find((entry) => entry.flowStepType === x))
    .filter((x): x is FlowStepCatalogEntry => !!x);

  return (
    <div className="flex flex-column gap-3">
      <div className="relative">
        <i
          className="pi pi-search absolute"
          style={{ left: 12, top: "50%", transform: "translateY(-50%)", ...MUTED }}
          aria-hidden="true"
        />
        <InputText
          value={query}
          onChange={(e) => search(e.target.value)}
          onKeyDown={onKeyDown}
          autoFocus
          placeholder="Search: click, wait, type, image, command..."
          aria-label="Search step types"
          className="w-full"
          style={{ paddingLeft: 36, paddingRight: 128 }}
        />
        <span
          className="absolute flex align-items-center gap-1"
          style={{ right: 10, top: "50%", transform: "translateY(-50%)", pointerEvents: "none" }}
          aria-hidden="true"
        >
          <KeyComponent text="↑" />
          <KeyComponent text="↓" />
          <KeyComponent text="Enter" />
        </span>
      </div>

      {!typed && recentEntries.length > 0 && (
        <div className="flex align-items-center flex-wrap gap-2 text-xs">
          <span style={MUTED}>Recent</span>
          {recentEntries.map((entry) => (
            <RecentChipComponent
              key={entry.flowStepType}
              entry={entry}
              onPick={() => pick(entry.flowStepType)}
            />
          ))}
        </div>
      )}

      {SECTIONS.map((section) => {
        const entries = shown.filter((x) => x.group === section.group);
        if (entries.length === 0) return null;

        const accent = flowStepAccent(entries[0].flowStepType);

        return (
          <section
            key={section.group}
            aria-label={section.title}
            className="flex flex-column gap-2"
          >
            <div className="flex align-items-center flex-wrap column-gap-2 row-gap-1">
              <span
                className="flex-none border-circle"
                style={{ width: 8, height: 8, background: accent }}
              />
              <span
                className="text-xs font-bold uppercase"
                style={{ color: accent, letterSpacing: "0.08em" }}
              >
                {section.title}
              </span>
              <span
                className="text-xs"
                style={MUTED}
              >
                {section.subtitle}
              </span>
            </div>

            <div style={ENTRIES}>
              {entries.map((entry) => (
                <EntryComponent
                  key={entry.flowStepType}
                  entry={entry}
                  isMarked={entry === marked}
                  onMark={() => setMarkedType(entry.flowStepType)}
                  onPick={() => pick(entry.flowStepType)}
                />
              ))}
            </div>
          </section>
        );
      })}

      {shown.length === 0 && (
        <span
          className="text-sm"
          style={MUTED}
        >
          {`No step matches “${query.trim()}”.`}
        </span>
      )}
    </div>
  );
}

interface EntryProps {
  entry: FlowStepCatalogEntry;
  isMarked: boolean;
  onMark: () => void;
  onPick: () => void;
}

/** Wears what its row in the tree will: the left edge, the tint and the icon's square. */
function EntryComponent({ entry, isMarked, onMark, onPick }: EntryProps) {
  const accent = flowStepAccent(entry.flowStepType);

  return (
    <button
      id={elementIdOf(entry.flowStepType)}
      type="button"
      onClick={onPick}
      onMouseEnter={onMark}
      className="flex align-items-start gap-2 w-full p-2 text-left cursor-pointer border-round-lg transition-colors transition-duration-150"
      style={{
        border: 0,
        borderLeft: `3px solid ${accent}`,
        background: tint(accent, isMarked ? 16 : 9),
        outline: isMarked ? "2px solid var(--primary-color)" : "none",
        outlineOffset: 1,
        color: "var(--text-color)",
        font: "inherit",
        minHeight: 60,
      }}
    >
      <span
        className="flex align-items-center justify-content-center flex-none border-round-md"
        style={{ width: 28, height: 28, color: accent, background: tint(accent, 18) }}
      >
        <i
          className={`pi pi-${entry.iconName} text-sm`}
          aria-hidden="true"
        />
      </span>

      <span className="flex flex-column gap-1 min-w-0">
        <span className="flex align-items-center flex-wrap gap-2 text-sm font-semibold">
          {entry.label}
          {hasBranches(entry.flowStepType) && <BranchesMarkComponent />}
        </span>
        <span
          className="text-xs line-height-3"
          style={MUTED}
        >
          {entry.description}
        </span>
      </span>
    </button>
  );
}

function BranchesMarkComponent() {
  return (
    <span
      className="inline-flex align-items-center gap-1 text-xs font-normal"
      style={MUTED}
      title="Comes with a Success and a Failure branch"
    >
      <span
        className="border-circle"
        style={{ width: 6, height: 6, background: flowStepAccent(FlowStepTypeEnum.SUCCESS) }}
      />
      <span
        className="border-circle"
        style={{ width: 6, height: 6, background: flowStepAccent(FlowStepTypeEnum.FAILURE) }}
      />
      branches
    </span>
  );
}

function RecentChipComponent({ entry, onPick }: { entry: FlowStepCatalogEntry; onPick: () => void }) {
  const accent = flowStepAccent(entry.flowStepType);

  return (
    <button
      type="button"
      onClick={onPick}
      className="inline-flex align-items-center gap-2 cursor-pointer"
      style={{
        padding: "3px 10px",
        borderRadius: 99,
        border: `1px solid ${tint(accent, 45)}`,
        background: tint(accent, 8),
        color: "var(--text-color)",
        font: "inherit",
      }}
    >
      <span
        className="border-circle"
        style={{ width: 7, height: 7, background: accent }}
      />
      {entry.label}
    </button>
  );
}

function KeyComponent({ text }: { text: string }) {
  return (
    <kbd
      className="text-xs border-round px-1"
      style={{
        ...MUTED,
        fontFamily: "inherit",
        border: "1px solid var(--surface-border)",
        borderBottomWidth: 2,
      }}
    >
      {text}
    </kbd>
  );
}
