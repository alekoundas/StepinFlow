import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Skeleton } from "primereact/skeleton";

import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";
import { chipStyle } from "@/features/execution/execution-format";
import { useFlowsWithIssues } from "@/features/flow/hooks/use-flow-health";
import { plural } from "@/features/repository/repository-format";
import HomeCardComponent from "@/features/home/components/HomeCardComponent";
import { MUTED, iconTile } from "@/features/home/home-format";

const ERROR_COLOR = "var(--red-400)";
const WARNING_COLOR = "var(--yellow-500)";
const CLEAR_COLOR = "var(--green-400)";

/** Every flow validation has something to say about. Clicking one opens it, where the messages are. */
export default function NeedsAttentionCardComponent() {
  const { flows, isLoading } = useFlowsWithIssues();

  let action = null;
  if (flows.length > 0)
    action = (
      <span
        className="text-sm white-space-nowrap"
        style={MUTED}
      >
        {plural(flows.length, "flow")}
      </span>
    );

  return (
    <HomeCardComponent
      icon="exclamation-triangle"
      title="Needs attention"
      action={action}
    >
      {isLoading && (
        <div className="flex flex-column gap-2">
          <Skeleton height="2.25rem" />
          <Skeleton height="2.25rem" />
          <Skeleton height="2.25rem" />
        </div>
      )}

      {!isLoading && flows.length === 0 && <AllClearComponent />}

      {flows.length > 0 && (
        <div
          className="flex flex-column gap-1 overflow-auto"
          style={{ maxHeight: 420 }}
        >
          {flows.map((flow) => (
            <IssueRowComponent
              key={flow.flowId}
              flow={flow}
            />
          ))}
        </div>
      )}
    </HomeCardComponent>
  );
}

function IssueRowComponent({ flow }: { flow: FlowHealthDto }) {
  const navigate = useNavigate();
  const [isHovered, setIsHovered] = useState(false);

  return (
    <button
      type="button"
      onClick={() => navigate(`/workflow/${flow.flowId}`)}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className="flex align-items-center gap-3 w-full p-2 border-none border-round-lg text-left cursor-pointer flex-none transition-colors transition-duration-150"
      style={{
        background: isHovered ? "color-mix(in srgb, var(--text-color) 7%, transparent)" : "transparent",
        color: "var(--text-color)",
        font: "inherit",
      }}
    >
      <i
        className={`pi pi-${flow.isSubFlow ? "clone" : "sitemap"} text-sm flex-none`}
        style={MUTED}
        aria-hidden="true"
      />
      <span
        className="flex-1 min-w-0 white-space-nowrap overflow-hidden text-overflow-ellipsis"
        title={flow.flowName}
      >
        {flow.flowName}
      </span>

      {flow.errorCount > 0 && <span style={chipStyle(ERROR_COLOR)}>{plural(flow.errorCount, "error")}</span>}
      {flow.warningCount > 0 && <span style={chipStyle(WARNING_COLOR)}>{plural(flow.warningCount, "warning")}</span>}

      <i
        className="pi pi-angle-right text-sm flex-none"
        style={MUTED}
        aria-hidden="true"
      />
    </button>
  );
}

function AllClearComponent() {
  return (
    <div className="flex flex-column align-items-center justify-content-center gap-2 flex-1 py-5 text-center">
      <span
        className="flex align-items-center justify-content-center border-circle"
        style={{ width: 44, height: 44, ...iconTile(CLEAR_COLOR) }}
      >
        <i
          className="pi pi-check"
          aria-hidden="true"
        />
      </span>
      <span className="font-semibold">No issues in any flow</span>
      <span
        className="text-sm"
        style={MUTED}
      >
        A flow with an error or a warning shows up here.
      </span>
    </div>
  );
}
