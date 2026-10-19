import { useState } from "react";
import type { CSSProperties, DragEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { ProgressBar } from "primereact/progressbar";
import { TabPanel, TabView } from "primereact/tabview";
import { Tag } from "primereact/tag";

import ScriptMergeComponent from "@/shared/components/script-view/ScriptMergeComponent";
import ScriptViewComponent from "@/shared/components/script-view/ScriptViewComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { FlowScriptFileDto } from "@/shared/models/flow-script-export-dtos";
import type { FlowScriptErrorDto, FlowScriptImportResultDto } from "@/shared/models/flow-script-import-result-dto";
import type { FlowScriptPreviewDto } from "@/shared/models/flow-script-preview-dto";

// The theme's own variables: PrimeFlex 4's surface and text-color classes read tokens this theme
// does not define.
const CARD: CSSProperties = { background: "var(--surface-card)", borderColor: "var(--surface-border)" };
const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };
const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

type VerdictKind = "new" | "replace" | "refused";

interface Verdict {
  kind: VerdictKind;
  title: string;
  text: string;
}

// A key per choice, so the same file chosen again after an edit is read again.
interface Picked {
  path: string;
  at: number;
}

/**
 * Import a flow from a .sflw file. The file is read first, the way an import reads it, and the
 * page says what importing would do - a new flow, the flow with the same Id replaced, or nothing
 * because the file does not read cleanly or its name is taken - beside the script with its
 * diagnostics marked. Nothing is saved until Import.
 */
export default function ImportFlowPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [picked, setPicked] = useState<Picked | null>(null);
  const [imported, setImported] = useState<FlowScriptImportResultDto | null>(null);
  const [isImporting, setIsImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: exportFolder } = useQuery({
    queryKey: ["flowScript", "exportFolder"],
    queryFn: () => backendApiService.FlowScript.getExportFolder(),
  });

  const {
    data: preview,
    isFetching: isReading,
    error: readError,
  } = useQuery({
    queryKey: ["flowScript", "preview", picked?.path, picked?.at],
    queryFn: () => backendApiService.FlowScript.preview(picked!.path),
    enabled: picked !== null,
    staleTime: 0,
  });

  const pick = (path: string) => {
    setError(null);
    setImported(null);
    setPicked({ path, at: Date.now() });
  };

  const choose = async () => {
    const path = await ElectronApiService.files.chooseScript(exportFolder?.folderPath);
    if (path) pick(path);
  };

  const drop = (event: DragEvent<HTMLElement>) => {
    event.preventDefault();

    const file = event.dataTransfer.files[0];
    if (!file) return;

    const path = ElectronApiService.files.pathOf(file);
    if (!path.toLowerCase().endsWith(".sflw")) {
      setError("Drop a .sflw file: a flow script exported from StepinFlow or written by hand.");
      return;
    }

    pick(path);
  };

  const importFile = async () => {
    if (!picked) return;

    setIsImporting(true);
    setError(null);

    try {
      const result = await backendApiService.FlowScript.import(picked.path);
      if (!result.isSuccess) {
        setError(`Nothing was imported. ${linesOf(result.errors)}`);
        return;
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["flows"] }),
        queryClient.invalidateQueries({ queryKey: ["flow"] }),
        queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
      ]);

      setImported(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsImporting(false);
    }
  };

  const again = () => {
    setPicked(null);
    setImported(null);
    setError(null);
    void queryClient.invalidateQueries({ queryKey: ["flowScript", "exportFolder"] });
  };

  const isChoosing = picked === null && imported === null;

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
          Import a flow
        </h1>
        <p
          className="m-0"
          style={{ ...MUTED, maxWidth: 760 }}
        >
          A .sflw script, with its templates in the folder named after it, beside it. It is read the way every script
          is, and nothing is saved until you press Import.
        </p>
      </div>

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      {readError && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={readError instanceof Error ? readError.message : String(readError)}
        />
      )}

      {isChoosing && (
        <ChooseComponent
          scripts={exportFolder?.scripts ?? []}
          folderPath={exportFolder?.folderPath ?? ""}
          onChoose={() => void choose()}
          onPick={pick}
          onDrop={drop}
        />
      )}

      {picked && !imported && isReading && (
        <div className="flex flex-column gap-2">
          <span
            className="text-sm"
            style={MUTED}
          >
            Reading {picked.path}
          </span>
          <ProgressBar
            mode="indeterminate"
            style={{ height: "0.375rem" }}
          />
        </div>
      )}

      {picked && !imported && !isReading && preview && (
        <PreviewComponent
          preview={preview}
          isImporting={isImporting}
          onChooseAnother={again}
          onCancel={() => navigate(-1)}
          onImport={() => void importFile()}
        />
      )}

      {imported && preview && (
        <DoneComponent
          imported={imported}
          preview={preview}
          onAgain={again}
          onOpen={() => navigate(`/workflow/${imported.flowId}`)}
        />
      )}
    </div>
  );
}

function ChooseComponent({
  scripts,
  folderPath,
  onChoose,
  onPick,
  onDrop,
}: {
  scripts: FlowScriptFileDto[];
  folderPath: string;
  onChoose: () => void;
  onPick: (path: string) => void;
  onDrop: (event: DragEvent<HTMLElement>) => void;
}) {
  return (
    <>
      <section
        aria-labelledby="choose-heading"
        className="flex flex-column align-items-center gap-3 p-5 border-2 border-dashed border-round-xl text-center"
        style={CARD}
        onDragOver={(e) => e.preventDefault()}
        onDrop={onDrop}
      >
        <i
          className="pi pi-file-import"
          style={{ fontSize: "2rem", color: "var(--primary-color)" }}
          aria-hidden="true"
        />
        <h2
          id="choose-heading"
          className="m-0 text-lg font-semibold"
        >
          Choose a script to import
        </h2>
        <p
          className="m-0"
          style={{ ...MUTED, maxWidth: 520 }}
        >
          Exported from StepinFlow or written by hand. The file dialog opens in the export folder; drop a file here
          instead if you have it open.
        </p>
        <Button
          type="button"
          label="Choose a file…"
          onClick={onChoose}
        />
      </section>

      <section
        aria-labelledby="folder-heading"
        className="border-1 border-round-xl overflow-hidden"
        style={CARD}
      >
        <div className="flex align-items-baseline justify-content-between flex-wrap gap-2 px-4 pt-3 pb-2">
          <h2
            id="folder-heading"
            className="m-0 text-base font-semibold"
          >
            In the export folder
          </h2>
          <span
            className="text-xs"
            style={{ ...MONO, ...MUTED }}
          >
            {folderPath}
          </span>
        </div>

        {scripts.length === 0 && (
          <p
            className="m-0 px-4 pb-3"
            style={MUTED}
          >
            Nothing exported yet. A flow&apos;s Export, in the editor, writes here unless told otherwise.
          </p>
        )}

        <ul className="list-none m-0 p-0">
          {scripts.map((script) => (
            <li
              key={script.path}
              className="border-top-1"
              style={{ borderColor: "var(--surface-border)" }}
            >
              <button
                type="button"
                onClick={() => onPick(script.path)}
                className="w-full flex align-items-center gap-3 px-4 py-3 border-none cursor-pointer text-left"
                style={{ background: "transparent", color: "inherit", font: "inherit" }}
              >
                <span
                  className="flex-1 min-w-0 text-sm"
                  style={MONO}
                >
                  {script.fileName}
                </span>
                <span
                  className="text-xs"
                  style={MUTED}
                >
                  {plural(script.templateCount, "template")}
                </span>
                <span
                  className="text-xs text-right"
                  style={{ ...MUTED, minWidth: 120 }}
                >
                  {whenOf(script.modifiedAt)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}

function PreviewComponent({
  preview,
  isImporting,
  onChooseAnother,
  onCancel,
  onImport,
}: {
  preview: FlowScriptPreviewDto;
  isImporting: boolean;
  onChooseAnother: () => void;
  onCancel: () => void;
  onImport: () => void;
}) {
  const verdict = verdictOf(preview);
  const found = preview.templateNames.length - preview.missingTemplates.length;
  const isReplacing = verdict.kind === "replace";

  let importLabel = "Import";
  if (isReplacing) importLabel = "Replace the flow";

  let footNote = "A warning does not stop an import.";
  if (isReplacing) footNote = "The file stays where it is; replacing the flow cannot be undone here.";
  if (verdict.kind === "refused") footNote = "Fix the file, then choose it again.";

  return (
    <>
      <section
        aria-label="The file"
        className="flex align-items-center flex-wrap gap-3 px-4 py-3 border-1 border-round-xl"
        style={CARD}
      >
        <i
          className="pi pi-file"
          style={MUTED}
          aria-hidden="true"
        />
        <span
          className="flex flex-column min-w-0"
          style={{ flex: "1 1 420px" }}
        >
          <span
            className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis"
            style={MONO}
          >
            {preview.scriptPath}
          </span>
          <span
            className="text-xs"
            style={MUTED}
          >
            Templates from {preview.templateFolderPath} · {found} of {preview.templateNames.length} found
          </span>
        </span>
        <Button
          type="button"
          label="Choose another…"
          onClick={onChooseAnother}
          severity="secondary"
          outlined
          size="small"
        />
      </section>

      <div className="flex flex-wrap align-items-start gap-4">
        <div
          className="flex flex-column gap-3"
          style={{ flex: "1 1 340px", minWidth: 0 }}
        >
          <VerdictComponent verdict={verdict} />
          <FlowComponent preview={preview} />
          {preview.errors.length + preview.warnings.length > 0 && <IssuesComponent preview={preview} />}
          <TemplatesComponent preview={preview} />
        </div>

        <section
          aria-label="The script"
          className="border-1 border-round-xl overflow-hidden"
          style={{ ...CARD, flex: "2 1 600px", minWidth: 0 }}
        >
          <TabView
            pt={{ panelContainer: { className: "p-0" } }}
            renderActiveOnly
          >
            <TabPanel header="Script">
              <ScriptViewComponent
                script={preview.script}
                diagnostics={[...preview.errors, ...preview.warnings]}
                maxHeight={720}
              />
            </TabPanel>
            {isReplacing && (
              <TabPanel header="Changes from the flow you have">
                <ChangesComponent preview={preview} />
              </TabPanel>
            )}
          </TabView>
        </section>
      </div>

      <div className="flex align-items-center justify-content-end flex-wrap gap-2">
        <span
          className="text-sm"
          style={{ ...MUTED, flex: "1 1 320px" }}
        >
          {footNote}
        </span>
        <Button
          type="button"
          label="Cancel"
          onClick={onCancel}
          severity="secondary"
          text
        />
        <Button
          type="button"
          label={importLabel}
          icon="pi pi-file-import"
          onClick={onImport}
          loading={isImporting}
          disabled={verdict.kind === "refused"}
        />
      </div>
    </>
  );
}

const VERDICT_COLORS: Record<VerdictKind, string> = {
  new: "var(--primary-color)",
  replace: "var(--yellow-400)",
  refused: "var(--red-400)",
};

function VerdictComponent({ verdict }: { verdict: Verdict }) {
  const color = VERDICT_COLORS[verdict.kind];

  return (
    <section
      aria-labelledby="verdict-heading"
      className="flex flex-column gap-1 p-3 border-1 border-round-xl"
      style={{
        borderColor: `color-mix(in srgb, ${color} 40%, transparent)`,
        background: `color-mix(in srgb, ${color} 6%, transparent)`,
      }}
    >
      <h2
        id="verdict-heading"
        className="m-0 text-base font-semibold"
        style={{ color }}
      >
        {verdict.title}
      </h2>
      <p
        className="m-0"
        style={MUTED}
      >
        {verdict.text}
      </p>
    </section>
  );
}

function FlowComponent({ preview }: { preview: FlowScriptPreviewDto }) {
  let issueSeverity: "success" | "warning" | "danger" = "success";
  if (preview.warnings.length > 0) issueSeverity = "warning";
  if (preview.errors.length > 0) issueSeverity = "danger";

  return (
    <section
      aria-labelledby="flow-heading"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        id="flow-heading"
        className="m-0 text-base font-semibold"
      >
        {preview.flowName || "No Flow: line"}
      </h2>
      {preview.description && (
        <p
          className="m-0"
          style={{ ...MUTED, whiteSpace: "pre-line" }}
        >
          {preview.description}
        </p>
      )}
      <div className="flex flex-wrap gap-2">
        <Tag
          value={plural(preview.stepCount, "step")}
          severity="secondary"
          rounded
        />
        <Tag
          value={plural(preview.inputs.length, "input")}
          severity="secondary"
          rounded
        />
        <Tag
          value={plural(preview.templateNames.length, "template")}
          severity="secondary"
          rounded
        />
        <Tag
          value={`${plural(preview.errors.length, "error")} · ${plural(preview.warnings.length, "warning")}`}
          severity={issueSeverity}
          rounded
        />
      </div>
      <span
        className="text-xs"
        style={{ ...MONO, ...MUTED }}
      >
        Id {preview.publicId}
      </span>
    </section>
  );
}

function IssuesComponent({ preview }: { preview: FlowScriptPreviewDto }) {
  const issues = [
    ...preview.errors.map((x) => ({ ...x, isError: true })),
    ...preview.warnings.map((x) => ({ ...x, isError: false })),
  ];

  return (
    <section
      aria-labelledby="issues-heading"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        id="issues-heading"
        className="m-0 text-base font-semibold"
      >
        Errors and warnings
      </h2>
      <ul className="list-none m-0 p-0 flex flex-column gap-2">
        {issues.map((issue, index) => (
          <li
            key={index}
            className="flex gap-2"
          >
            <i
              className={issue.isError ? "pi pi-times-circle" : "pi pi-exclamation-triangle"}
              style={{ marginTop: 3, color: issue.isError ? "var(--red-400)" : "var(--yellow-400)" }}
              aria-hidden="true"
            />
            <span className="flex flex-column">
              <span className="text-sm font-medium">
                Line {issue.line}, column {issue.column}
              </span>
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

function TemplatesComponent({ preview }: { preview: FlowScriptPreviewDto }) {
  const missing = new Set(preview.missingTemplates.map((x) => x.toLowerCase()));

  return (
    <section
      aria-labelledby="templates-heading"
      className="flex flex-column gap-2 p-3 border-1 border-round-xl"
      style={CARD}
    >
      <h2
        id="templates-heading"
        className="m-0 text-base font-semibold"
      >
        Templates and inputs
      </h2>

      {preview.templateNames.length === 0 && (
        <span
          className="text-sm"
          style={MUTED}
        >
          No templates.
        </span>
      )}

      <ul className="list-none m-0 p-0 flex flex-column gap-1">
        {preview.templateNames.map((name) => {
          const isMissing = missing.has(name.toLowerCase());

          return (
            <li
              key={name}
              className="flex align-items-center gap-2 text-sm"
            >
              <i
                className={isMissing ? "pi pi-exclamation-triangle" : "pi pi-check"}
                style={{ color: isMissing ? "var(--yellow-400)" : "var(--green-400)" }}
                aria-hidden="true"
              />
              <span
                className="flex-1 min-w-0"
                style={MONO}
              >
                {name}
              </span>
              <span
                className="text-xs"
                style={{ color: isMissing ? "var(--yellow-400)" : "var(--text-color-secondary)" }}
              >
                {isMissing ? "not in the folder: imported without a picture" : "found"}
              </span>
            </li>
          );
        })}
      </ul>

      {preview.inputs.length > 0 && (
        <>
          <div style={{ height: 1, background: "var(--surface-border)" }} />
          <ul className="list-none m-0 p-0 flex flex-column gap-1 text-sm">
            {preview.inputs.map((input) => (
              <li
                key={input.name}
                className="flex gap-2"
              >
                <span style={{ ...MONO, color: "var(--green-400)", minWidth: 110 }}>{`{{${input.name}}}`}</span>
                <span style={MUTED}>
                  {input.isSecret
                    ? "secret: not in the file, asked for when the flow executes"
                    : `default ${input.defaultValue || "(empty)"}`}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

// The flow as it is, beside the file, folded to what differs.
function ChangesComponent({ preview }: { preview: FlowScriptPreviewDto }) {
  if (normalized(preview.replacedScript) === normalized(preview.script))
    return (
      <p
        className="m-0 p-4"
        style={MUTED}
      >
        The file is the same as the flow you have.
      </p>
    );

  return (
    <ScriptMergeComponent
      original={preview.replacedScript}
      script={preview.script}
      originalLabel="The flow you have"
      scriptLabel="The file"
      maxHeight={720}
    />
  );
}

function DoneComponent({
  imported,
  preview,
  onAgain,
  onOpen,
}: {
  imported: FlowScriptImportResultDto;
  preview: FlowScriptPreviewDto;
  onAgain: () => void;
  onOpen: () => void;
}) {
  const errors = (imported.validation?.issues ?? []).filter((x) => x.severity === "ERROR").length;

  let text = `Imported ${imported.flowName} as a new flow: ${plural(preview.stepCount, "step")}.`;
  if (preview.target.flowId !== null) text = `Replaced ${imported.flowName} with the file: ${plural(preview.stepCount, "step")}. Its execution history is kept.`;
  if (imported.missingTemplates.length > 0) text += ` ${plural(imported.missingTemplates.length, "template")} came without a picture.`;
  if (errors > 0) text += ` It has ${plural(errors, "error")} to fix before it can execute.`;

  return (
    <div className="flex flex-column gap-3">
      <Message
        severity="success"
        className="w-full justify-content-start"
        text={text}
      />
      <div className="flex justify-content-end gap-2">
        <Button
          type="button"
          label="Import another"
          onClick={onAgain}
          severity="secondary"
          text
        />
        <Button
          type="button"
          label="Open in the editor"
          icon="pi pi-arrow-right"
          iconPos="right"
          onClick={onOpen}
        />
      </div>
    </div>
  );
}

// What the import's write path would do with this file, as the preview found it.
function verdictOf(preview: FlowScriptPreviewDto): Verdict {
  if (preview.errors.length > 0)
    return {
      kind: "refused",
      title: "The file does not read cleanly",
      text: "Nothing can be imported until the errors are fixed in the file and it is chosen again. They are marked at their lines in the script.",
    };

  if (preview.target.nameTaken)
    return {
      kind: "refused",
      title: "The name is taken",
      text: `${preview.target.nameTaken} Rename one of them - the Flow: line in the file, or the flow here - and choose the file again.`,
    };

  if (preview.target.flowId !== null) {
    let history = "It has never executed.";
    if (preview.target.executionCount > 0)
      history = `Its ${plural(preview.target.executionCount, "execution")} stay in the history; the last good screenshots of its steps go.`;

    return {
      kind: "replace",
      title: "Replaces a flow you have",
      text: `${preview.target.flowName} here has the same Id, so the file takes its place: its steps, areas, points and inputs become the file's. ${history}`,
    };
  }

  return {
    kind: "new",
    title: "A new flow",
    text: "No flow here has this Id, so it is added beside the others. It never executes on its own: open it and read it first.",
  };
}

function linesOf(errors: FlowScriptErrorDto[]): string {
  return errors.map((x) => `line ${x.line}: ${x.message}`).join(" ");
}

function normalized(script: string): string {
  return script.replace(/\r\n/g, "\n").trimEnd();
}

// "Today, 18:20", "Yesterday, 09:05", or the date.
function whenOf(value: string): string {
  const date = new Date(value);
  const time = date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

  const today = new Date();
  const yesterday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1);

  if (date.toDateString() === today.toDateString()) return `Today, ${time}`;
  if (date.toDateString() === yesterday.toDateString()) return `Yesterday, ${time}`;

  return date.toLocaleDateString();
}

function plural(count: number, one: string): string {
  return `${count} ${count === 1 ? one : `${one}s`}`;
}
