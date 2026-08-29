import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";

import { DataGridComponent } from "@/shared/components/DataGridComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import IconComponent from "@/shared/components/IconComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowDto } from "@/shared/models/database/flow-dto";
import FlowHealthBadgeComponent from "@/features/flow/components/FlowHealthBadgeComponent";
import { useDeleteFlow } from "@/features/flow/hooks/use-delete-flow";
import { useFlowHealth } from "@/features/flow/hooks/use-flow-health";

type Props = {
  className?: string;

  /** Which side of the flag to list. The two pages never show each other. */
  isSubFlow: boolean;
};

/**
 * Four things per card: name, description, size, health.
 *
 * Deliberately fewer than the table. A card is read at a glance, and areas and points are too
 * internal to earn the space here — density is what the table is for.
 */
export function FlowDataGridComponent({ className, isSubFlow }: Props) {
  const navigate = useNavigate();
  const deleteFlow = useDeleteFlow();

  const health = useFlowHealth();

  const cardTemplate = (item: FlowDto) => {
    return (
      <div
        className="flex flex-column gap-2 p-3 h-full"
        onClick={() => navigate(`/workflow/${item.id}`)}
      >
        <div className="flex align-items-start justify-content-between gap-2">
          <LabelComponent
            text={item.name}
            weight="semibold"
            className="flex-1 min-w-0"
          />

          <div className="flex align-items-center gap-1 flex-shrink-0">
            <FlowHealthBadgeComponent
              health={health.get(item.id)}
              isEmpty={item.stepCount === 0}
            />
            <Button
              type="button"
              icon="pi pi-play"
              text
              className="p-button-sm"
              aria-label="Run"
              onClick={(e) => {
                e.stopPropagation();
                navigate(`/execution/${item.id}`);
              }}
            />
            <Button
              type="button"
              icon="pi pi-trash"
              text
              className="p-button-sm p-button-danger"
              aria-label="Delete"
              onClick={(e) => {
                e.stopPropagation();
                deleteFlow(item);
              }}
            />
          </div>
        </div>

        {/* The one field that makes a list of flows readable, so it gets the room. */}
        <LabelComponent
          text={item.description || "No description yet."}
          size="sm"
          color="secondary"
        />

        <div className="flex align-items-center gap-3 mt-auto pt-2">
          <span className="flex align-items-center gap-1">
            <IconComponent
              name="list"
              size="sm"
            />
            <LabelComponent
              text={`${item.stepCount}`}
              size="xs"
              color="secondary"
            />
          </span>

          {isSubFlow && (
            <span className="flex align-items-center gap-1">
              <IconComponent
                name="sitemap"
                size="sm"
              />
              <LabelComponent
                text={`used by ${item.callerCount}`}
                size="xs"
                color="secondary"
              />
            </span>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className={className}>
      <DataGridComponent<FlowDto>
        queryKey={["flows", "list", isSubFlow]}
        queryFn={(dto) => backendApiService.Flow.getLazy({ ...dto, isSubFlow })}
        itemTemplate={cardTemplate}
        enablePaging={true}
      />
    </div>
  );
}
