import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import StatusPillComponent from "@/shared/components/StatusPillComponent";
import type { FlowExecutionSummaryDto } from "@/shared/models/database/flow-execution-summary-dto";
import { dayAndTime } from "@/shared/utils/day-and-time";
import HomeHeroComponent from "@/features/home/components/HomeHeroComponent";

interface Props {
  flow: FlowExecutionSummaryDto | undefined;
  isExecuting: boolean;
}

/** Execute goes to the flow's execution page and stops there: starting it is that page's button. */
export default function LastExecutedCardComponent({ flow, isExecuting }: Props) {
  const navigate = useNavigate();

  if (!flow)
    return (
      <HomeHeroComponent
        eyebrow="Nothing executed yet"
        title="Pick a flow to execute"
      >
        <Button
          type="button"
          label="Open flows"
          icon="pi pi-sitemap"
          size="large"
          onClick={() => navigate("/flows")}
        />
      </HomeHeroComponent>
    );

  let eyebrow = <>Last executed</>;
  if (flow.lastRunOn) eyebrow = <>Last executed · {dayAndTime(flow.lastRunOn)}</>;
  if (isExecuting)
    eyebrow = (
      <StatusPillComponent
        text="Executing"
        severity="running"
        pulse
      />
    );

  return (
    <HomeHeroComponent
      eyebrow={eyebrow}
      title={flow.flowName}
    >
      <Button
        type="button"
        label={isExecuting ? "View execution" : "Execute"}
        icon={isExecuting ? "pi pi-eye" : "pi pi-play"}
        size="large"
        onClick={() => navigate(`/execution/${flow.flowId}`)}
      />
    </HomeHeroComponent>
  );
}
