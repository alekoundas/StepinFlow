import type { CSSProperties } from "react";

import PageComponent from "@/shared/components/PageComponent";
import FirstFlowCardComponent from "@/features/home/components/FirstFlowCardComponent";
import LastExecutedCardComponent from "@/features/home/components/LastExecutedCardComponent";
import LocalModelCardComponent from "@/features/home/components/LocalModelCardComponent";
import NeedsAttentionCardComponent from "@/features/home/components/NeedsAttentionCardComponent";
import RepositoryCardComponent from "@/features/home/components/RepositoryCardComponent";
import UpdatesCardComponent from "@/features/home/components/UpdatesCardComponent";
import { useLastExecutedFlow } from "@/features/home/hooks/use-last-executed-flow";

const COLUMNS: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(360px, 1fr))",
  gap: "1rem",
};

// The first card takes what the other column leaves, so both columns end together.
const FIRST_CARD_FILLS: CSSProperties = {
  display: "grid",
  gridTemplateRows: "1fr auto",
  gap: "1rem",
  minWidth: 0,
};

/**
 * What to do next and what is in the way of it: the flow last executed, the flows with issues, the
 * repository, the local model and the updates. Each card reads what it shows for itself and keeps
 * itself up to date, so none of them waits for the others.
 */
export default function HomePage() {
  const { flow, isExecuting, hasFlows, isLoading } = useLastExecutedFlow();

  const isNewInstall = !isLoading && !hasFlows;

  return (
    <PageComponent
      width="wide"
      className="flex flex-column gap-3"
    >
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

          <div className="flex flex-column gap-3 min-w-0">
            <LocalModelCardComponent />
            <UpdatesCardComponent />
          </div>
        </div>
      ) : (
        <div style={COLUMNS}>
          <div style={FIRST_CARD_FILLS}>
            <NeedsAttentionCardComponent />
            <UpdatesCardComponent />
          </div>

          <div className="flex flex-column gap-3 min-w-0">
            <RepositoryCardComponent />
            <LocalModelCardComponent />
          </div>
        </div>
      )}
    </PageComponent>
  );
}
