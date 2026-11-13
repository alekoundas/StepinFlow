import type { CSSProperties } from "react";
import { Skeleton } from "primereact/skeleton";

import PageComponent from "@/shared/components/PageComponent";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import FirstFlowCardComponent from "@/features/home/components/FirstFlowCardComponent";
import HomeHeroComponent from "@/features/home/components/HomeHeroComponent";
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

const CARD_RADIUS = "1rem";

/**
 * What to do next and what is in the way of it: the flow last executed, the flows with issues, the
 * repository, the local model and the updates. Each card reads what it shows for itself and keeps
 * itself up to date, so none of them waits for the others.
 *
 * Which cards there are depends on whether there is any flow, so until the summaries say, it shows
 * placeholders. The backend answers one request at a time, and cards mounted early would put their
 * requests ahead of the summaries.
 */
export default function HomePage() {
  const { flow, isExecuting, hasFlows, isLoading } = useLastExecutedFlow();

  let content = <PlaceholdersComponent />;
  if (!isLoading && !hasFlows) content = <NewInstallComponent />;
  if (!isLoading && hasFlows)
    content = (
      <WithFlowsComponent
        flow={flow}
        isExecuting={isExecuting}
      />
    );

  return (
    <PageComponent
      width="wide"
      className="flex flex-column gap-3"
    >
      {content}
    </PageComponent>
  );
}

function PlaceholdersComponent() {
  return (
    <>
      <HomeHeroComponent
        eyebrow={
          <Skeleton
            width="8rem"
            height="1rem"
          />
        }
        title={
          <Skeleton
            width="16rem"
            height="2rem"
          />
        }
      />

      <div style={COLUMNS}>
        <Skeleton
          height="22rem"
          borderRadius={CARD_RADIUS}
        />

        <div className="flex flex-column gap-3 min-w-0">
          <Skeleton
            height="10.5rem"
            borderRadius={CARD_RADIUS}
          />
          <Skeleton
            height="10.5rem"
            borderRadius={CARD_RADIUS}
          />
        </div>
      </div>
    </>
  );
}

function NewInstallComponent() {
  return (
    <>
      <FirstFlowCardComponent />

      <div style={COLUMNS}>
        <RepositoryCardComponent />

        <div className="flex flex-column gap-3 min-w-0">
          <LocalModelCardComponent />
          <UpdatesCardComponent />
        </div>
      </div>
    </>
  );
}

function WithFlowsComponent({ flow, isExecuting }: { flow: FlowExecutionSummaryDto | undefined; isExecuting: boolean }) {
  return (
    <>
      <LastExecutedCardComponent
        flow={flow}
        isExecuting={isExecuting}
      />

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
    </>
  );
}
