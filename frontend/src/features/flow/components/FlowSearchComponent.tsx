import { useEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { AutoComplete } from "primereact/autocomplete";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowSearchHitDto } from "@/shared/models/flow-search-hit-dto";
import {
  flowStepAccent,
  getFlowStepCatalogEntry,
} from "@/shared/models/flow-step-catalog";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { MONO } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface HitGroup {
  label: string;
  hits: FlowSearchHitDto[];
}

// PrimeReact types a grouped list's templates with one type for the groups and their items.
type Suggestion = HitGroup | FlowSearchHitDto;

/**
 * "Search flows and steps" in the header, focused by Ctrl K. A flow opens in the editor; a step
 * opens its flow with the tree opened down to it and the step picked.
 */
export default function FlowSearchComponent() {
  const navigate = useNavigate();
  const setStepToReveal = useWorkflowStore((x) => x.setStepToReveal);
  const input = useRef<HTMLInputElement>(null);

  const [text, setText] = useState<string>("");
  const [groups, setGroups] = useState<HitGroup[]>([]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== "k")
        return;

      event.preventDefault();
      input.current?.focus();
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  const search = async (query: string) => {
    const hits = await backendApiService.Flow.search(query);

    const found: HitGroup[] = [
      { label: "Flows", hits: hits.filter((x) => x.flowStepId === null) },
      { label: "Steps", hits: hits.filter((x) => x.flowStepId !== null) },
    ];
    setGroups(found.filter((x) => x.hits.length > 0));
  };

  const open = (hit: FlowSearchHitDto) => {
    setText("");
    setGroups([]);
    input.current?.blur();

    if (hit.flowStepId !== null) setStepToReveal(hit.flowStepId);
    navigate(`/workflow/${hit.flowId}`);
  };

  return (
    <div
      className="flex align-items-center gap-2 px-2 border-1 surface-border border-round-lg surface-card"
      style={{ width: 340, maxWidth: "100%", height: 36 }}
    >
      <i
        className="pi pi-search text-sm"
        style={MUTED}
        aria-hidden="true"
      />
      <AutoComplete
        inputRef={input}
        value={text}
        suggestions={groups}
        completeMethod={(e) => void search(e.query)}
        onChange={(e) => setText(typeof e.value === "string" ? e.value : "")}
        onSelect={(e) => open(e.value as FlowSearchHitDto)}
        optionGroupLabel="label"
        optionGroupChildren="hits"
        optionGroupTemplate={(group: Suggestion) =>
          "hits" in group && (
            <span
              className="text-xs font-semibold"
              style={MUTED}
            >
              {group.label}
            </span>
          )
        }
        itemTemplate={(hit: Suggestion) =>
          !("hits" in hit) && <HitComponent hit={hit} />
        }
        delay={200}
        placeholder="Search flows and steps"
        aria-label="Search flows and steps"
        className="flex-1 min-w-0"
        inputClassName="w-full text-sm"
        inputStyle={{
          background: "transparent",
          border: 0,
          boxShadow: "none",
          padding: 0,
        }}
        emptyMessage="Nothing found"
        showEmptyMessage
      />
      <kbd
        className="text-xs border-1 surface-border border-round px-1 white-space-nowrap"
        style={{ ...MUTED, fontFamily: MONO }}
      >
        Ctrl K
      </kbd>
    </div>
  );
}

function HitComponent({ hit }: { hit: FlowSearchHitDto }) {
  if (hit.flowStepId === null)
    return (
      <div className="flex align-items-center gap-2">
        <i
          className={`pi ${hit.isSubFlow ? "pi-clone" : "pi-sitemap"} text-sm`}
          style={MUTED}
          aria-hidden="true"
        />
        <span>{hit.flowName}</span>
        <span
          className="text-xs"
          style={MUTED}
        >
          {kindOf(hit)}
        </span>
      </div>
    );

  const step = getFlowStepCatalogEntry(hit.flowStepType ?? undefined);

  return (
    <div className="flex align-items-center gap-2">
      <i
        className={`pi pi-${step?.iconName ?? "circle"} text-sm`}
        style={{ color: flowStepAccent(hit.flowStepType ?? undefined) }}
        aria-hidden="true"
      />
      <span>{hit.flowStepName}</span>
      <span
        className="text-xs"
        style={MUTED}
      >
        in {hit.flowName}
      </span>
    </div>
  );
}

const kindOf = (hit: FlowSearchHitDto): string => {
  if (hit.isSubFlow) return "Sub-flow";
  if (hit.isQaTest) return "QA test";
  return "Automation";
};
