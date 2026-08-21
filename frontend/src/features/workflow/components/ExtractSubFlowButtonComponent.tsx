import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  node: TreeNodeDto;
  rootFlowId: number;
  onExtracted: (result: { flowStepId: number; parentId: number; isFlow: boolean }) => void;
}

/**
 * Lifts this step and everything under it into a new sub-flow, leaving a Sub-Flow step behind.
 *
 * This is how sub-flows actually get made: nobody plans them, they notice a repeat. Refused when
 * a reference would end up with one end on each side of the split, because clearing it silently
 * would break a step in a flow the user is not looking at.
 */
export default function ExtractSubFlowButtonComponent({
  node,
  rootFlowId,
  onExtracted,
}: Props) {
  const queryClient = useQueryClient();
  const { openForm, closeAll } = useDialogStore();
  const [error, setError] = useState<string | null>(null);

  const extract = async (name: string) => {
    setError(null);

    try {
      const result = await backendApiService.Flow.extractSubFlow({
        flowStepId: node.entityId,
        name,
        sourceRootId: rootFlowId,
        sourceFlowId: node.parentFlowStepId ? undefined : node.parentFlowId,
        sourceParentFlowStepId: node.parentFlowStepId ?? undefined,
        sourceOrderNumber: node.orderNumber,
      });

      closeAll();
      await queryClient.invalidateQueries({ queryKey: ["flow"] });
      await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });

      onExtracted({
        flowStepId: result.flowStepId,
        parentId: node.parentFlowStepId ?? node.parentFlowId ?? rootFlowId,
        isFlow: node.parentFlowStepId == null,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const open = () =>
    openForm("extract-sub-flow", {
      headerText: "Extract to sub-flow",
      formId: "extract-sub-flow",
      children: <ExtractDialog defaultName={node.name} onSubmit={extract} />,
    });

  return (
    <>
      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start mb-3"
          text={error}
        />
      )}

      <Button
        type="button"
        label="Extract to sub-flow"
        icon="pi pi-sitemap"
        onClick={open}
        className="p-button-outlined p-button-sm"
        tooltip="Move this step and everything under it into a reusable sub-flow"
        tooltipOptions={{ position: "left" }}
      />
    </>
  );
}

function ExtractDialog({
  defaultName,
  onSubmit,
}: {
  defaultName: string;
  onSubmit: (name: string) => Promise<void>;
}) {
  const [name, setName] = useState(defaultName);
  const [isSaving, setIsSaving] = useState(false);

  const submit = async () => {
    if (name.trim().length === 0) return;

    setIsSaving(true);
    try {
      await onSubmit(name.trim());
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex flex-column gap-3">
      <LabelComponent
        text="This step and everything under it moves into a new sub-flow, and a Sub-Flow step takes its place."
        size="sm"
        color="secondary"
      />

      <LabelComponent
        text="The search areas and points it uses are copied, so the sub-flow works from any flow. Later edits to the originals will not reach the copies."
        size="xs"
        color="secondary"
      />

      <InputText
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Name the sub-flow"
        autoFocus
      />

      <div className="flex justify-content-end">
        <Button
          type="button"
          label={isSaving ? "Extracting..." : "Extract"}
          icon="pi pi-check"
          loading={isSaving}
          disabled={name.trim().length === 0 || isSaving}
          onClick={() => void submit()}
        />
      </div>
    </div>
  );
}
