import type { CSSProperties } from "react";

import FirstFlowCardComponent from "@/features/home/components/FirstFlowCardComponent";
import LastExecutedCardComponent from "@/features/home/components/LastExecutedCardComponent";
import LocalModelCardComponent from "@/features/home/components/LocalModelCardComponent";
import NeedsAttentionCardComponent from "@/features/home/components/NeedsAttentionCardComponent";
import RepositoryCardComponent from "@/features/home/components/RepositoryCardComponent";
import { useLastExecutedFlow } from "@/features/home/hooks/use-last-executed-flow";

const COLUMNS: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(360px, 1fr))",
  gap: "1rem",
};

/**
 * What to do next and what is in the way of it: the flow last executed, the flows with issues, the
 * repository and the local model. Each card reads what it shows for itself and keeps itself up to
 * date, so none of them waits for the others.
 */
export default function HomePage() {
  const { flow, isExecuting, hasFlows, isLoading } = useLastExecutedFlow();

  const isNewInstall = !isLoading && !hasFlows;

  return (
    <div
      className="m-4 flex flex-column gap-3"
      style={{ maxWidth: 1240 }}
    >
      <h1
        className="m-0 text-3xl font-semibold"
        style={{ letterSpacing: "-0.02em" }}
      >
        Home
      </h1>

      {isNewInstall ? (
        <FirstFlowCardComponent />
      ) : (
        <LastExecutedCardComponent
          flow={flow}
          isExecuting={isExecuting}
          isLoading={isLoading}
        />
      )}

      {isNewInstall ? (
        <div style={COLUMNS}>
          <RepositoryCardComponent />
          <LocalModelCardComponent />
        </div>
      ) : (
        <div style={COLUMNS}>
          <NeedsAttentionCardComponent />

          <div className="flex flex-column gap-3 min-w-0">
            <RepositoryCardComponent />
            <LocalModelCardComponent />
          </div>
        </div>
      )}
    </div>
  );
}
