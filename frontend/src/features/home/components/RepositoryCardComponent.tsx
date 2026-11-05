import { useState } from "react";
import { Button } from "primereact/button";
import { Skeleton } from "primereact/skeleton";

import { dayAndTime } from "@/shared/utils/day-and-time";
import BranchMenuComponent from "@/features/repository/components/BranchMenuComponent";
import CommitDialogComponent from "@/features/repository/components/CommitDialogComponent";
import { useRepositoryStatus } from "@/features/repository/hooks/use-repository";
import { UNCOMMITTED_COLOR } from "@/features/repository/repository-format";
import HomeCardComponent from "@/features/home/components/HomeCardComponent";
import HomeLinkComponent from "@/features/home/components/HomeLinkComponent";
import { MUTED } from "@/features/home/home-format";

const ICON = "history";
const TITLE = "Repository";

/** The branch and its menu, and what is waiting to go anywhere. A change stops a switch, so Commit is here too. */
export default function RepositoryCardComponent() {
  const { data: status, isPending } = useRepositoryStatus();
  const [isCommitting, setIsCommitting] = useState(false);

  if (!status)
    return (
      <HomeCardComponent
        icon={ICON}
        title={TITLE}
      >
        {isPending ? (
          <Skeleton height="4.5rem" />
        ) : (
          <span
            className="text-sm"
            style={MUTED}
          >
            No repository yet. It is made when the app starts, and may still be writing the flows.
          </span>
        )}
      </HomeCardComponent>
    );

  return (
    <HomeCardComponent
      icon={ICON}
      title={TITLE}
      action={
        <div
          className="border-1 border-round-md"
          style={{ borderColor: "var(--surface-border)" }}
        >
          <BranchMenuComponent
            flowId={undefined}
            branch={status.branch}
            commitCount={status.commitCount}
            changeCount={status.changeCount}
          />
        </div>
      }
    >
      <div className="flex gap-2">
        <StatTileComponent
          value={status.changeCount}
          label="Not committed"
          dotColor={status.changeCount > 0 ? UNCOMMITTED_COLOR : undefined}
        />

        {status.remoteUrl ? (
          <>
            <StatTileComponent
              value={status.aheadCount}
              label="To push"
            />
            <StatTileComponent
              value={status.behindCount}
              label="To pull"
            />
          </>
        ) : (
          <StatTileComponent
            value={status.commitCount}
            label="Commits"
          />
        )}
      </div>

      <div className="flex align-items-center justify-content-between gap-2">
        <span
          className="text-xs min-w-0"
          style={MUTED}
        >
          {status.lastCommitAt ? `Last commit ${dayAndTime(status.lastCommitAt)}` : "Nothing committed yet"}
        </span>

        <div className="flex align-items-center flex-none gap-1">
          {status.changeCount > 0 && (
            <Button
              type="button"
              label="Commit"
              icon="pi pi-check-circle"
              size="small"
              outlined
              onClick={() => setIsCommitting(true)}
            />
          )}
          <HomeLinkComponent
            to="/settings/repository"
            label="Open"
          />
        </div>
      </div>

      <CommitDialogComponent
        visible={isCommitting}
        flowId={undefined}
        onHide={() => setIsCommitting(false)}
      />
    </HomeCardComponent>
  );
}

interface StatTileProps {
  value: number;
  label: string;

  /** Beside the label, so the number itself stays in the text colour. */
  dotColor?: string;
}

function StatTileComponent({ value, label, dotColor }: StatTileProps) {
  return (
    <div
      className="flex flex-column gap-1 flex-1 min-w-0 p-3 border-round-xl"
      style={{ background: "var(--surface-ground)" }}
    >
      <span
        className="text-2xl font-semibold"
        style={{ letterSpacing: "-0.02em" }}
      >
        {value}
      </span>
      <span
        className="flex align-items-center gap-2 text-xs white-space-nowrap"
        style={MUTED}
      >
        {dotColor && (
          <span
            className="flex-none border-circle"
            style={{ width: 7, height: 7, background: dotColor }}
          />
        )}
        {label}
      </span>
    </div>
  );
}
