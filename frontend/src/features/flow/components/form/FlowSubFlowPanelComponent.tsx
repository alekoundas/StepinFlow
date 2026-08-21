import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";

interface Props {
  flowId: number;
  isSubFlow: boolean;
}

/**
 * Whether this flow can be called, and by whom.
 *
 * "Used by" is here because editing a sub-flow three flows depend on should be a decision rather
 * than something noticed afterwards.
 */
export default function FlowSubFlowPanelComponent({ flowId, isSubFlow }: Props) {
  const queryClient = useQueryClient();
  const { openConfirm } = useDialogStore();
  const [error, setError] = useState<string | null>(null);

  const { data: callers = [] } = useQuery({
    queryKey: ["flow", "callers", flowId],
    queryFn: () => backendApiService.Flow.getCallers(flowId),
    enabled: flowId > 0 && isSubFlow,
  });

  // One way, so the confirmation has to say both that it sticks and where the flow is going.
  // "My flow disappeared" is the shape the mistake takes.
  const promote = () =>
    openConfirm("flow-promote", {
      headerText: "Make this a sub-flow?",
      confirmLabel: "Make it a sub-flow",
      confirmSeverity: "warning",
      children: (
        <LabelComponent text="It moves out of Flows and into Sub-Flows, and other flows will be able to run it as a step. This cannot be undone." />
      ),
      onConfirm: async () => {
        setError(null);

        try {
          await backendApiService.Flow.promoteToSubFlow(flowId);
          await queryClient.invalidateQueries({ queryKey: ["flow"] });
          await queryClient.invalidateQueries({ queryKey: ["flows", "list"] });
        } catch (err) {
          setError(err instanceof Error ? err.message : String(err));
        }
      },
    });

  if (!isSubFlow) {
    return (
      <Panel
        header="Reuse"
        toggleable
        collapsed
        className="mt-3"
      >
        <LabelComponent
          text="Making this a sub-flow lets other flows run it as a single step. It moves to the Sub-Flows list and cannot be moved back."
          size="sm"
          color="secondary"
        />

        {error && (
          <Message
            severity="error"
            className="w-full justify-content-start mt-3"
            text={error}
          />
        )}

        <div className="mt-3">
          <Button
            type="button"
            label="Make this a sub-flow"
            icon="pi pi-sitemap"
            onClick={promote}
            className="p-button-outlined"
          />
        </div>
      </Panel>
    );
  }

  return (
    <Panel
      header="Used by"
      toggleable
      className="mt-3"
    >
      {callers.length === 0 ? (
        <LabelComponent
          text="Nothing runs this yet. Add a Sub-Flow step to a flow to use it."
          size="sm"
          color="secondary"
        />
      ) : (
        <div className="flex flex-wrap gap-2">
          {callers.map((caller) => (
            <Tag
              key={caller.value}
              value={caller.label}
              severity={caller.description === "sub-flow" ? "info" : undefined}
            />
          ))}
        </div>
      )}
    </Panel>
  );
}
