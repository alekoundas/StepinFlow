import { Tag } from "primereact/tag";

import IconComponent from "@/shared/components/IconComponent";
import type { FlowHealthDto } from "@/shared/models/database/flow-health-dto";

interface Props {
  health: FlowHealthDto | undefined;
  isEmpty: boolean;
}

/**
 * Whether the flow would run. The question you actually have when looking at a list of them.
 *
 * Nothing is shown while the counts are still loading rather than a placeholder that flickers
 * into a different answer a moment later.
 */
export default function FlowHealthBadgeComponent({ health, isEmpty }: Props) {
  if (isEmpty)
    return (
      <Tag
        value="Empty"
        severity="secondary"
      />
    );

  if (!health) return null;

  if (health.errorCount > 0)
    return (
      <Tag severity="danger">
        <IconComponent
          name="times-circle"
          size="sm"
        />
        <span className="ml-1">{health.errorCount}</span>
      </Tag>
    );

  if (health.warningCount > 0)
    return (
      <Tag severity="warning">
        <IconComponent
          name="exclamation-triangle"
          size="sm"
        />
        <span className="ml-1">{health.warningCount}</span>
      </Tag>
    );

  return (
    <Tag severity="success">
      <IconComponent
        name="check"
        size="sm"
      />
    </Tag>
  );
}
