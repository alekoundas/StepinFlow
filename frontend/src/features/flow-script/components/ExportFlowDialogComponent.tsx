import { useState } from "react";
import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Message } from "primereact/message";

import { backendApiService } from "@/shared/services/backend-api-service";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { FlowScriptExportResultDto } from "@/shared/models/flow-script-export-dtos";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };
const MONO: CSSProperties = { fontFamily: "ui-monospace, Consolas, monospace" };

interface Props {
  flowId: number;
  flowName: string;
  visible: boolean;
  onHide: () => void;
}

/**
 * Writes the flow as a .sflw script, with its templates in a folder of the same name beside it.
 * The folder starts as the app's export folder, where an import's file dialog opens too.
 */
export default function ExportFlowDialogComponent({ flowId, flowName, visible, onHide }: Props) {
  const [chosen, setChosen] = useState<string | null>(null);
  const [exported, setExported] = useState<FlowScriptExportResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  const { data: exportFolder } = useQuery({
    queryKey: ["flowScript", "exportFolder"],
    queryFn: () => backendApiService.FlowScript.getExportFolder(),
    enabled: visible,
  });

  const folder = chosen ?? exportFolder?.folderPath ?? "";

  const browse = async () => {
    const picked = await ElectronApiService.files.chooseFolder(folder || undefined);
    if (picked) setChosen(picked);
  };

  const exportFlow = async () => {
    setIsExporting(true);
    setError(null);

    try {
      setExported(await backendApiService.FlowScript.export(flowId, folder.trim()));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsExporting(false);
    }
  };

  // Opened again, it starts over in the export folder.
  const hide = () => {
    setChosen(null);
    setExported(null);
    setError(null);
    onHide();
  };

  let footer = (
    <div className="flex justify-content-end gap-2">
      <Button
        type="button"
        label="Cancel"
        onClick={hide}
        severity="secondary"
        text
      />
      <Button
        type="button"
        label="Export"
        icon="pi pi-file-export"
        onClick={() => void exportFlow()}
        loading={isExporting}
        disabled={folder.trim().length === 0}
      />
    </div>
  );

  if (exported)
    footer = (
      <div className="flex justify-content-end gap-2">
        <Button
          type="button"
          label="Show in folder"
          icon="pi pi-folder-open"
          onClick={() => void ElectronApiService.files.showInFolder(exported.scriptPath)}
          severity="secondary"
          outlined
        />
        <Button
          type="button"
          label="Done"
          onClick={hide}
        />
      </div>
    );

  return (
    <Dialog
      header={`Export ${flowName}`}
      visible={visible}
      onHide={hide}
      footer={footer}
      style={{ width: "min(640px, 92vw)" }}
      modal
    >
      {!exported && (
        <div className="flex flex-column gap-3">
          <p
            className="m-0"
            style={MUTED}
          >
            Writes the flow as a .sflw script, with its templates in a folder of the same name beside it. A script
            of the same name already there is replaced.
          </p>

          <div className="flex flex-column gap-2">
            <label
              htmlFor="export-folder"
              className="font-medium"
            >
              Folder
            </label>
            <div className="flex gap-2">
              <InputText
                id="export-folder"
                value={folder}
                onChange={(e) => setChosen(e.target.value)}
                className="flex-1 min-w-0"
                style={MONO}
              />
              <Button
                type="button"
                label="Browse…"
                onClick={() => void browse()}
                severity="secondary"
                outlined
              />
            </div>
            {exportFolder && folder !== exportFolder.folderPath && (
              <Button
                type="button"
                label="Back to the export folder"
                onClick={() => setChosen(null)}
                className="align-self-start p-0"
                size="small"
                link
              />
            )}
          </div>

          {error && (
            <Message
              severity="error"
              className="w-full justify-content-start"
              text={error}
            />
          )}
        </div>
      )}

      {exported && (
        <div className="flex flex-column gap-2">
          <Message
            severity="success"
            className="w-full justify-content-start"
            text={`Exported ${flowName}.`}
          />
          <span
            className="text-sm"
            style={{ ...MONO, wordBreak: "break-all" }}
          >
            {exported.scriptPath}
          </span>
          <span
            className="text-sm"
            style={MUTED}
          >
            {templatesOf(exported)}
          </span>
        </div>
      )}
    </Dialog>
  );
}

function templatesOf(exported: FlowScriptExportResultDto): string {
  if (exported.templateCount === 0) return "No templates.";
  if (exported.templateCount === 1) return `1 template, in ${exported.templateFolderPath}`;

  return `${exported.templateCount} templates, in ${exported.templateFolderPath}`;
}
