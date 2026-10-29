import { useMemo, useState } from "react";
import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import DisabledTooltipComponent from "@/shared/components/DisabledTooltipComponent";
import { InputTextarea } from "primereact/inputtextarea";
import { Message } from "primereact/message";
import { ProgressBar } from "primereact/progressbar";
import { TabPanel, TabView } from "primereact/tabview";
import { Tag } from "primereact/tag";
import type { TreeExpandedKeysType } from "primereact/tree";

import ReadOnlyFlowTreeComponent from "@/shared/components/data-tree/ReadOnlyFlowTreeComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowGenerationStageEnum } from "@/shared/enums/backend/flow-generation-stage-enum";
import { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import type { FlowGenerationProgressDto } from "@/shared/models/flow-generation-progress-dto";
import type { FlowGenerationResultDto } from "@/shared/models/flow-generation-result-dto";
import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import ScriptMergeComponent from "@/shared/components/script-view/ScriptMergeComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import { RefusedComponent } from "@/features/recording/generate/RecordingGeneratingComponent";

// The theme's own variables: PrimeFlex 4's surface and text-color classes read tokens this theme
// does not define.
const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

// A note is quieter than a warning.
const SEVERITY_LOOK: Record<ValidationSeverityEnum, { icon: string; color: string }> = {
  [ValidationSeverityEnum.ERROR]: { icon: "pi pi-times-circle", color: "var(--red-400)" },
  [ValidationSeverityEnum.WARNING]: { icon: "pi pi-exclamation-triangle", color: "var(--yellow-400)" },
  [ValidationSeverityEnum.INFO]: { icon: "pi pi-info-circle", color: "var(--text-color-secondary)" },
};

/** Why the last "Ask for changes" left the flow as it was. */
export interface RevisionProblem {
  message: string;
  refused: FlowScriptErrorDto[];
}

interface Issue {
  severity: ValidationSeverityEnum;
  where: string;
  message: string;
}

interface Props {
  result: FlowGenerationResultDto;
  revision: FlowGenerationProgressDto | null;
  revisionProblem: RevisionProblem | null;
  askUnavailable: string | null;
  onAsk: (changes: string) => void;
  onCancelAsk: () => void;
  onDiscard: () => void;
  onOpen: () => void;
}

/**
 * What was saved, read before it ever executes: the script with what the model changed marked, the
 * tree, the model's notes, and what the import and the validator said about it. "Ask for changes"
 * sends the script back with what the tester wrote, and the answer is checked the way the first one
 * was; one that never reads cleanly leaves the flow as it was.
 */
export default function RecordingCheckComponent({
  result,
  revision,
  revisionProblem,
  askUnavailable,
  onAsk,
  onCancelAsk,
  onDiscard,
  onOpen,
}: Props) {
  const issues = issuesOf(result);
  const errors = issues.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length;
  const warnings = issues.filter((x) => x.severity === ValidationSeverityEnum.WARNING).length;
  const notes = issues.filter((x) => x.severity === ValidationSeverityEnum.INFO).length;
  const isAsking = revision !== null;

  let issueCounts = `${plural(errors, "error")} · ${plural(warnings, "warning")}`;
  if (notes > 0) issueCounts += ` · ${plural(notes, "note")}`;

  let issueSeverity: "success" | "warning" | "danger" = "success";
  if (warnings > 0) issueSeverity = "warning";
  if (errors > 0) issueSeverity = "danger";

  let heading = "Check the draft";
  if (result.isModelDraft) heading = "Check what was generated";

  // What the model was given, so what it changed shows. A plain draft changed nothing.
  let changedFrom = "";
  if (result.isModelDraft) changedFrom = result.draft;

  return (
    <div className="flex flex-column gap-4">
      <div className="flex align-items-end justify-content-between flex-wrap gap-3">
        <div className="flex flex-column gap-2">
          <h2
            className="m-0 text-2xl font-semibold"
            style={{ letterSpacing: "-0.02em" }}
          >
            {heading}
          </h2>
          <div className="flex flex-wrap gap-2">
            <Tag
              value={plural(result.stepCount, "step")}
              severity="secondary"
              rounded
            />
            <Tag
              value={plural(result.inputCount, "input")}
              severity="secondary"
              rounded
            />
            <Tag
              value={plural(result.templateCount, "template")}
              severity="secondary"
              rounded
            />
            <Tag
              value={issueCounts}
              severity={issueSeverity}
              rounded
            />
            <Tag
              value="Never executed"
              severity="secondary"
              rounded
            />
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            label="Discard the flow"
            onClick={onDiscard}
            disabled={isAsking}
            severity="danger"
            text
          />
          <Button
            type="button"
            label="Open in the editor"
            icon="pi pi-arrow-right"
            iconPos="right"
            onClick={onOpen}
            disabled={isAsking}
          />
        </div>
      </div>

      <div className="flex flex-wrap align-items-start gap-4">
        <div
          className="flex flex-column gap-3"
          style={{ flex: "1 1 320px", minWidth: 0 }}
        >
          <WhatWasDoneComponent result={result} />

          {issues.length > 0 && <IssuesComponent issues={issues} />}

          <section
            aria-labelledby="before-heading"
            className="flex flex-column gap-1 p-3 border-1 border-round-xl"
            style={{
              borderColor: "color-mix(in srgb, var(--yellow-500) 40%, transparent)",
              background: "color-mix(in srgb, var(--yellow-500) 6%, transparent)",
            }}
          >
            <h3
              id="before-heading"
              className="m-0 text-sm font-semibold"
              style={{ color: "var(--yellow-400)" }}
            >
              Read it before it executes
            </h3>
            <p
              className="m-0"
              style={MUTED}
            >
              A generated flow never executes on its own. Read it in the editor, then execute it once while you watch.
            </p>
          </section>

          <AskComponent
            result={result}
            revision={revision}
            problem={revisionProblem}
            unavailable={askUnavailable}
            onAsk={onAsk}
            onCancel={onCancelAsk}
          />
        </div>

        <section
          aria-label="The saved flow"
          className="border-1 border-round-xl overflow-hidden"
          style={{ ...CARD, flex: "2 1 600px", minWidth: 0 }}
        >
          <TabView
            pt={{ panelContainer: { className: "p-0" } }}
            renderActiveOnly
          >
            <TabPanel header="Script">
              <ScriptViewComponent
                script={result.script}
                original={changedFrom || undefined}
                diagnostics={result.import.warnings}
              />
              {changedFrom && (
                <div
                  className="flex flex-wrap gap-4 px-3 py-2 border-top-1 text-xs"
                  style={{ ...MUTED, borderColor: "var(--surface-border)" }}
                >
                  <span>
                    <span style={{ color: "var(--green-400)" }}>Green</span>: written by the model
                  </span>
                  <span>
                    <span style={{ color: "var(--red-400)" }}>Red</span>: the draft&apos;s lines it replaced
                  </span>
                </div>
              )}
            </TabPanel>
            <TabPanel header="Tree">
              <CheckTreeComponent flowId={result.import.flowId} />
            </TabPanel>
            {changedFrom && (
              <TabPanel header="Changes from the draft">
                <ChangesComponent
                  draft={changedFrom}
                  script={result.script}
                />
              </TabPanel>
            )}
          </TabView>
        </section>
      </div>
    </div>
  );
}

// The model's notes, or why there are none.
function WhatWasDoneComponent({ result }: { result: FlowGenerationResultDto }) {
  if (result.reason)
    return (
      <Message
        severity="warn"
        className="w-full justify-content-start"
        text={`The model's answer was not saved, so this is the plain draft. ${result.reason}`}
      />
    );

  if (!result.isModelDraft)
    return (
      <section
        aria-labelledby="done-heading"
        className="flex flex-column gap-2 p-3 border-1 border-round-xl"
        style={CARD}
      >
        <h3
          id="done-heading"
          className="m-0 text-base font-semibold"
        >
          The plain draft
        </h3>
        <p
          className="m-0"
          style={MUTED}
        >
          Written by rules, without a model: each click waits for what it clicked, then clicks it, and each search is
          named from the text under it. Ask for changes below to have a model shape it.
        </p>
      </section>
    );

  return (
    <section
      aria-labelledby="done-heading"
      className="flex flex-column gap-3 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h3
        id="done-heading"
        className="m-0 text-base font-semibold"
      >
        What the model did
      </h3>

      {result.notes.length === 0 && (
        <p
          className="m-0"
          style={MUTED}
        >
          The model left no notes.
        </p>
      )}

      {result.notes.length > 0 && (
        <ol className="list-none m-0 p-0 flex flex-column gap-3">
          {result.notes.map((note, index) => (
            <li
              key={note}
              className="flex gap-2"
            >
              <span
                className="flex flex-none align-items-center justify-content-center border-round text-xs font-bold"
                style={{ width: 22, height: 22, background: "var(--surface-hover)", color: "var(--primary-color)" }}
              >
                {index + 1}
              </span>
              <span style={MUTED}>{note}</span>
            </li>
          ))}
        </ol>
      )}

      <p
        className="m-0 px-3 py-2 border-round-lg text-sm"
        style={{ ...MUTED, background: "var(--surface-ground)" }}
      >
        Every template is one cut from your recording, and every input is one you typed.
      </p>
    </section>
  );
}

// The import's warnings at their lines, then what the validator says about the saved flow.
function IssuesComponent({ issues }: { issues: Issue[] }) {
  return (
    <section
      aria-labelledby="issues-heading"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h3
        id="issues-heading"
        className="m-0 text-base font-semibold"
      >
        Errors, warnings and notes
      </h3>
      <ul className="list-none m-0 p-0 flex flex-column gap-2">
        {issues.map((issue, index) => (
          <li
            key={index}
            className="flex gap-2"
          >
            <i
              className={SEVERITY_LOOK[issue.severity].icon}
              style={{ marginTop: 3, color: SEVERITY_LOOK[issue.severity].color }}
              aria-hidden="true"
            />
            <span className="flex flex-column">
              <span className="text-sm font-medium">{issue.where}</span>
              <span
                className="text-sm"
                style={MUTED}
              >
                {issue.message}
              </span>
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function AskComponent({
  result,
  revision,
  problem,
  unavailable,
  onAsk,
  onCancel,
}: {
  result: FlowGenerationResultDto;
  revision: FlowGenerationProgressDto | null;
  problem: RevisionProblem | null;
  unavailable: string | null;
  onAsk: (changes: string) => void;
  onCancel: () => void;
}) {
  const [changes, setChanges] = useState("");
  const [shown, setShown] = useState(result);

  // A revision saved: what was asked is in the script now.
  if (shown !== result) {
    setShown(result);
    setChanges("");
  }

  return (
    <section
      aria-labelledby="ask-heading"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h3
        id="ask-heading"
        className="m-0 text-base font-semibold"
      >
        Ask for changes
      </h3>
      <label
        htmlFor="ask-changes"
        className="text-sm"
        style={MUTED}
      >
        The model gets this script back with what you write, and the result is checked the same way.
      </label>
      <InputTextarea
        id="ask-changes"
        value={changes}
        onChange={(e) => setChanges(e.target.value)}
        rows={3}
        autoResize
        disabled={revision !== null}
        placeholder="Also sign out at the end, and name the loop after the invoices"
      />

      {revision && (
        <div className="flex flex-column gap-2">
          <span
            className="text-sm"
            style={MUTED}
          >
            {describeRevision(revision)}
          </span>
          <ProgressBar
            mode="indeterminate"
            style={{ height: "0.375rem" }}
          />
          {revision.refused.length > 0 && (
            <RefusedComponent
              refused={revision.refused}
              note={`Sent back to the model with the lines. Attempt ${revision.attempt} of ${revision.attemptCount}.`}
            />
          )}
        </div>
      )}

      {problem && !revision && (
        <div className="flex flex-column gap-2">
          <Message
            severity="warn"
            className="w-full justify-content-start"
            text={problem.message}
          />
          {problem.refused.length > 0 && <RefusedComponent refused={problem.refused} />}
        </div>
      )}

      <div className="flex gap-2">
        {!revision && (
          <DisabledTooltipComponent tooltip={unavailable}>
            <Button
              type="button"
              label="Regenerate"
              icon="pi pi-refresh"
              onClick={() => onAsk(changes.trim())}
              disabled={unavailable !== null || changes.trim().length === 0}
              size="small"
              severity="secondary"
              outlined
            />
          </DisabledTooltipComponent>
        )}
        {revision && (
          <Button
            type="button"
            label="Cancel"
            onClick={onCancel}
            size="small"
            severity="secondary"
            text
          />
        )}
      </div>
    </section>
  );
}

// Side by side, folded to what changed.
function ChangesComponent({ draft, script }: { draft: string; script: string }) {
  if (linesOf(draft) === linesOf(script))
    return (
      <p
        className="m-0 p-4"
        style={MUTED}
      >
        The model changed nothing.
      </p>
    );

  return (
    <ScriptMergeComponent
      original={draft}
      script={script}
      originalLabel="The draft it was given"
      scriptLabel="Saved"
    />
  );
}

// The whole flow at once, every branch open: there is nothing to edit here, only to read.
function CheckTreeComponent({ flowId }: { flowId: number }) {
  const { data: treeNodes } = useQuery({
    queryKey: ["flowStep", "treeNodesRecursive", flowId],
    queryFn: () => backendApiService.FlowStep.getTreeNodesRecursive(flowId),
    enabled: flowId > 0,
  });

  const expandedKeys = useMemo(() => {
    const keys: TreeExpandedKeysType = {};
    collectKeys(treeNodes ?? [], keys);
    return keys;
  }, [treeNodes]);

  return (
    <div className="p-2 overflow-auto" style={{ maxHeight: 640 }}>
      <ReadOnlyFlowTreeComponent
        value={treeNodes ?? []}
        expandedKeys={expandedKeys}
      />
    </div>
  );
}

function collectKeys(nodes: TreeNodeDto[], keys: TreeExpandedKeysType): void {
  for (const node of nodes) {
    keys[node.key] = true;
    collectKeys(node.children ?? [], keys);
  }
}

function issuesOf(result: FlowGenerationResultDto): Issue[] {
  const fromImport: Issue[] = result.import.warnings.map((x) => ({
    severity: ValidationSeverityEnum.WARNING,
    where: `Line ${x.line}`,
    message: x.message,
  }));

  const fromValidation: Issue[] = (result.import.validation?.issues ?? []).map((x) => ({
    severity: x.severity,
    where: x.flowStepName || "The flow",
    message: x.message,
  }));

  return [...fromValidation, ...fromImport].sort((a, b) => severityOrder(a.severity) - severityOrder(b.severity));
}

function severityOrder(severity: ValidationSeverityEnum): number {
  if (severity === ValidationSeverityEnum.ERROR) return 0;
  if (severity === ValidationSeverityEnum.WARNING) return 1;
  return 2;
}

function describeRevision(revision: FlowGenerationProgressDto): string {
  const model = revision.model || "the model";

  if (revision.stage === FlowGenerationStageEnum.SAVING) return "Saving the flow";
  if (revision.stage === FlowGenerationStageEnum.ASKING_MODEL)
    return `Asking ${model} · attempt ${revision.attempt} of ${revision.attemptCount}`;

  return "Reading the flow";
}

function linesOf(script: string): string {
  return script.replace(/\r\n/g, "\n").trimEnd();
}

function plural(count: number, one: string): string {
  return `${count} ${count === 1 ? one : `${one}s`}`;
}
