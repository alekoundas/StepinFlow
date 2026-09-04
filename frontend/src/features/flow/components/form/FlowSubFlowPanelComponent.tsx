import { useQuery } from "@tanstack/react-query";
import { Panel } from "primereact/panel";
import { Tag } from "primereact/tag";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";

interface Props {
  flowId: number;
  isSubFlow: boolean;
}

/**
 * Who runs this sub-flow.
 *
 * Here because editing a sub-flow three flows depend on should be a decision rather than something
 * noticed afterwards. A flow nothing can call has nobody to list, so it shows nothing at all -
 * making one into a sub-flow is an action on the flow, and lives with the other ones.
 */
export default function FlowSubFlowPanelComponent({ flowId, isSubFlow }: Props) {
  const { data: callers = [] } = useQuery({
    queryKey: ["flow", "callers", flowId],
    queryFn: () => backendApiService.Flow.getCallers(flowId),
    enabled: flowId > 0 && isSubFlow,
  });

  if (!isSubFlow) return null;

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
