import { useRef, useState } from "react";
import type { CSSProperties, FormEvent } from "react";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { Message } from "primereact/message";
import { OverlayPanel } from "primereact/overlaypanel";

import LabelComponent from "@/shared/components/LabelComponent";
import type { RepositoryBranchDto } from "@/shared/models/repository-dtos";
import BranchSwitchDialogComponent from "@/features/repository/components/BranchSwitchDialogComponent";
import { useCreateBranch, useRepositoryBranches, useSwitchBranch } from "@/features/repository/hooks/use-repository";
import { MONO, formatWhen, plural } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

interface Props {
  flowId: number | undefined;
  branch: string;
  commitCount: number;

  /** Every file not committed yet, whichever flow it belongs to: any one stops a switch. */
  changeCount: number;
}

/** The branch in the editor header, and its menu: the branches to switch to, and a new one. */
export default function BranchMenuComponent({ flowId, branch, commitCount, changeCount }: Props) {
  const panel = useRef<OverlayPanel>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [isNaming, setIsNaming] = useState(false);
  const [name, setName] = useState("");
  const [switchingTo, setSwitchingTo] = useState<string | null>(null);

  const { data: branches, error } = useRepositoryBranches(isOpen);
  const create = useCreateBranch();
  const switchBranch = useSwitchBranch(flowId);

  const pick = (target: RepositoryBranchDto) => {
    if (target.isCurrent || changeCount > 0) return;

    panel.current?.hide();
    setSwitchingTo(target.name);
    switchBranch.mutate(target.name, { onSuccess: () => setSwitchingTo(null) });
  };

  const closeSwitch = () => {
    setSwitchingTo(null);
    switchBranch.reset();
  };

  const createBranch = (event: FormEvent) => {
    event.preventDefault();

    create.mutate(name, {
      onSuccess: () => {
        setName("");
        panel.current?.hide();
      },
    });
  };

  const closeMenu = () => {
    setIsOpen(false);
    setIsNaming(false);
    create.reset();
  };

  return (
    <>
      <Button
        type="button"
        onClick={(event) => panel.current?.toggle(event)}
        text
        size="small"
        className="gap-2 px-3"
        style={{ color: "var(--text-color)", fontFamily: MONO }}
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label={`Branch ${branch}`}
      >
        <BranchIconComponent />
        {branch}
        <i
          className="pi pi-angle-down text-xs"
          style={MUTED}
          aria-hidden="true"
        />
      </Button>

      <OverlayPanel
        ref={panel}
        onShow={() => setIsOpen(true)}
        onHide={closeMenu}
        style={{ width: 330 }}
      >
        <div
          role="menu"
          aria-label="Branches"
          className="flex flex-column gap-1"
        >
          <span
            className="text-xs px-2 pb-1"
            style={MUTED}
          >
            Branches
          </span>

          {changeCount > 0 && (
            <Message
              severity="warn"
              text={`Commit or discard the ${plural(changeCount, "change")} to switch branch.`}
              className="mb-1"
            />
          )}

          {error && (
            <LabelComponent
              text={error instanceof Error ? error.message : String(error)}
              color="error"
            />
          )}

          {branches?.map((x) => (
            <BranchRowComponent
              key={x.name}
              branch={x}
              commitCount={commitCount}
              isLocked={!x.isCurrent && changeCount > 0}
              onPick={() => pick(x)}
            />
          ))}

          <div
            className="my-1"
            style={{ height: 1, background: "var(--surface-border)" }}
          />

          {isNaming ? (
            <form
              className="flex flex-column gap-2 p-2"
              onSubmit={createBranch}
            >
              <label
                htmlFor="new-branch-name"
                className="text-xs"
                style={MUTED}
              >
                New branch from <span style={{ fontFamily: MONO }}>{branch}</span>
              </label>
              <div className="flex gap-2">
                <InputText
                  id="new-branch-name"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  autoFocus
                  className="flex-1 p-inputtext-sm"
                  style={{ fontFamily: MONO }}
                />
                <Button
                  type="submit"
                  label="Create"
                  size="small"
                  loading={create.isPending}
                  disabled={name.trim().length === 0}
                />
              </div>
              {create.error && (
                <LabelComponent
                  text={create.error.message}
                  color="error"
                />
              )}
            </form>
          ) : (
            <button
              type="button"
              role="menuitem"
              onClick={() => setIsNaming(true)}
              className="flex align-items-center gap-2 p-2 border-none border-round-md text-left cursor-pointer"
              style={{ background: "transparent", color: "var(--text-color)", font: "inherit" }}
            >
              <i
                className="pi pi-plus text-xs"
                aria-hidden="true"
              />
              <span className="text-sm">
                New branch from <span style={{ fontFamily: MONO }}>{branch}</span>
              </span>
            </button>
          )}

          <p
            className="m-0 px-2 pt-1 text-xs"
            style={MUTED}
          >
            Switching brings every flow to that branch’s version. A flow it doesn’t have is hidden, never deleted.
          </p>
        </div>
      </OverlayPanel>

      {switchingTo !== null && (
        <BranchSwitchDialogComponent
          branch={switchingTo}
          error={switchBranch.error?.message ?? null}
          onClose={closeSwitch}
        />
      )}
    </>
  );
}

interface RowProps {
  branch: RepositoryBranchDto;
  commitCount: number;
  isLocked: boolean;
  onPick: () => void;
}

function BranchRowComponent({ branch, commitCount, isLocked, onPick }: RowProps) {
  let background = "transparent";
  if (branch.isCurrent) background = "color-mix(in srgb, var(--primary-color) 10%, transparent)";

  let cursor = "pointer";
  if (branch.isCurrent || isLocked) cursor = "default";

  return (
    <button
      type="button"
      role="menuitemradio"
      aria-checked={branch.isCurrent}
      aria-disabled={isLocked}
      onClick={onPick}
      className="flex align-items-center gap-2 p-2 border-none border-round-md text-left"
      style={{
        background,
        cursor,
        color: isLocked ? "var(--text-color-secondary)" : "var(--text-color)",
        font: "inherit",
      }}
    >
      <span
        className="flex-none"
        style={{ width: 14 }}
      >
        {branch.isCurrent && (
          <i
            className="pi pi-check text-xs"
            style={{ color: "var(--primary-color)" }}
            aria-hidden="true"
          />
        )}
      </span>
      <span
        className="flex-1 text-sm"
        style={{ fontFamily: MONO }}
      >
        {branch.name}
      </span>
      <span
        className="text-xs"
        style={MUTED}
      >
        {branch.isCurrent ? plural(commitCount, "commit") : formatWhen(branch.lastCommitAt)}
      </span>
    </button>
  );
}

// PrimeIcons has no branch.
function BranchIconComponent() {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle
        cx="6"
        cy="5"
        r="2"
      />
      <circle
        cx="6"
        cy="19"
        r="2"
      />
      <circle
        cx="18"
        cy="7"
        r="2"
      />
      <path d="M6 7v10" />
      <path d="M18 9c0 5-6 4-12 8" />
    </svg>
  );
}
