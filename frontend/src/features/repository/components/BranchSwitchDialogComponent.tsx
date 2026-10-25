import { useEffect, useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { Message } from "primereact/message";
import { ProgressBar } from "primereact/progressbar";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";
import type { RepositoryBranchSwitchDto, RepositoryBranchSwitchFlowDto } from "@/shared/models/repository-dtos";
import { MONO } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  /** "Switching to", "Pulling": what the title says is happening to the branch. */
  action: string;
  branch: string;

  /** Why the switch did not happen. Nothing changed then, and the dialog can be closed. */
  error: string | null;
  onClose: () => void;
}

/**
 * Holds the app while a branch switch or a pull brings the flows across, flow by flow. Mounted for
 * one of them, so it starts with nothing to show.
 */
export default function BranchSwitchDialogComponent({ action, branch, error, onClose }: Props) {
  const [progress, setProgress] = useState<RepositoryBranchSwitchDto | null>(null);

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.REPOSITORY_BRANCH_SWITCH) return;

      setProgress(message.payload as RepositoryBranchSwitchDto);
    });

    return () => {
      unsubscribe?.();
    };
  }, []);

  const flows = progress?.flows ?? [];
  const done = flows.filter((x) => x.isDone).length;
  const current = flows.find((x) => !x.isDone);

  return (
    <Dialog
      visible
      onHide={onClose}
      closable={error !== null}
      closeOnEscape={error !== null}
      modal
      style={{ width: "min(560px, 94vw)" }}
      header={
        <span className="flex align-items-center gap-3">
          {error === null && (
            <i
              className="pi pi-spin pi-spinner"
              style={{ color: "var(--primary-color)" }}
              aria-hidden="true"
            />
          )}
          <span>
            {action} <span style={{ fontFamily: MONO }}>{branch}</span>
          </span>
        </span>
      }
      footer={
        error !== null && (
          <Button
            type="button"
            label="Close"
            onClick={onClose}
          />
        )
      }
    >
      <div
        className="flex flex-column gap-3"
        aria-busy={error === null}
      >
        {error !== null ? (
          <Message
            severity="error"
            text={error}
          />
        ) : (
          <>
            <span style={MUTED}>Bringing every flow to that branch’s version. Editing and executing wait until it finishes.</span>

            <div className="flex flex-column gap-2">
              <ProgressBar
                mode={flows.length === 0 ? "indeterminate" : "determinate"}
                value={flows.length === 0 ? 0 : Math.round((done / flows.length) * 100)}
                showValue={false}
                style={{ height: 6 }}
                aria-label="Flows brought across"
              />
              <span
                className="text-xs"
                style={MUTED}
              >
                {captionOf(progress, current, done)}
              </span>
            </div>

            {flows.length > 0 && (
              <ul className="list-none m-0 p-0 flex flex-column gap-1">
                {flows.map((flow, index) => (
                  <FlowRowComponent
                    key={index}
                    flow={flow}
                    isCurrent={flow === current}
                  />
                ))}
              </ul>
            )}
          </>
        )}

        <span
          className="pt-3 border-top-1 surface-border text-xs"
          style={MUTED}
        >
          Only the flows that differ between the two branches are touched. A hidden flow keeps its executions, and comes back on
          any branch that has it.
        </span>
      </div>
    </Dialog>
  );
}

interface RowProps {
  flow: RepositoryBranchSwitchFlowDto;
  isCurrent: boolean;
}

function FlowRowComponent({ flow, isCurrent }: RowProps) {
  let icon = (
    <span
      style={MUTED}
      aria-hidden="true"
    >
      •
    </span>
  );

  if (flow.isDone)
    icon = (
      <i
        className="pi pi-check text-sm"
        style={{ color: "var(--green-400)" }}
        aria-hidden="true"
      />
    );
  else if (isCurrent)
    icon = (
      <i
        className="pi pi-spin pi-spinner text-sm"
        style={{ color: "var(--primary-color)" }}
        aria-hidden="true"
      />
    );

  return (
    <li
      className="flex align-items-center gap-3 px-1"
      style={{ minHeight: 34 }}
    >
      <span
        className="flex justify-content-center flex-none"
        style={{ width: 18 }}
      >
        {icon}
      </span>
      <span
        className="flex-1 min-w-0"
        style={flow.isDone || isCurrent ? undefined : MUTED}
      >
        {flow.name}
      </span>
      <span
        className="text-xs white-space-nowrap"
        style={MUTED}
      >
        {noteOf(flow, isCurrent)}
      </span>
    </li>
  );
}

// "sub-flow · new on this branch · importing"
const noteOf = (flow: RepositoryBranchSwitchFlowDto, isCurrent: boolean): string => {
  const isHidden = flow.change === GitChangeKindEnum.DELETED;
  const parts: string[] = [];

  if (flow.isSubFlow) parts.push("sub-flow");

  if (isHidden) parts.push("not on this branch");
  else if (flow.change === GitChangeKindEnum.ADDED) parts.push("new on this branch");
  else parts.push("changed");

  if (flow.isDone) parts.push(isHidden ? "hidden" : "imported");
  else if (isCurrent) parts.push(isHidden ? "hiding" : "importing");
  else parts.push(isHidden ? "to hide" : "to import");

  return parts.join(" · ");
};

// "3 of 4 · importing Checkout smoke"
const captionOf = (
  progress: RepositoryBranchSwitchDto | null,
  current: RepositoryBranchSwitchFlowDto | undefined,
  done: number,
): string => {
  if (!progress) return "Reading what differs between the two branches...";

  const total = progress.flows.length;
  if (total === 0) return "No flow differs between the two branches.";
  if (!current) return `${total} of ${total} · done`;

  const doing = current.change === GitChangeKindEnum.DELETED ? "hiding" : "importing";
  return `${done + 1} of ${total} · ${doing} ${current.name}`;
};
