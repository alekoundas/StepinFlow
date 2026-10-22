import { Button } from "primereact/button";
import { ListBox } from "primereact/listbox";
import { Tag } from "primereact/tag";

import IconComponent from "@/shared/components/IconComponent";

/** One row of an editor tab's list: an area, a location or an input. */
export interface WorkflowPartItem {
  id: number;
  icon: string;
  name: string;
  detail: string;
  usedCount: number;
  chip?: string;
  /** A region sits under the window it is inside. */
  depth?: number;
}

/** What a tab's list selects: a saved part, the form for a new one, or nothing yet. */
export type WorkflowPartSelection = number | "new" | null;

interface Props {
  note: string;
  addLabel: string;
  emptyText: string;
  items: WorkflowPartItem[];
  selected: WorkflowPartSelection;
  onSelect: (selected: WorkflowPartSelection) => void;
}

/** The left side of the Areas, Locations and Inputs tabs: the parts, and Add. */
export function WorkflowPartListComponent({
  note,
  addLabel,
  emptyText,
  items,
  selected,
  onSelect,
}: Props) {
  const itemTemplate = (item: WorkflowPartItem) => (
    <div
      className="flex align-items-center gap-3 w-full min-w-0"
      style={{ paddingLeft: `${(item.depth ?? 0) * 1.5}rem` }}
    >
      <span className="flex align-items-center justify-content-center flex-shrink-0 w-2rem h-2rem border-round-sm surface-hover text-color-secondary">
        <IconComponent
          name={item.icon}
          size="sm"
        />
      </span>

      <div className="flex flex-column flex-1 min-w-0">
        <span className="flex align-items-center gap-2 min-w-0">
          <span className="font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {item.name}
          </span>
          {item.chip && (
            <Tag
              value={item.chip}
              severity="secondary"
            />
          )}
        </span>
        <span className="text-xs text-color-secondary white-space-nowrap overflow-hidden text-overflow-ellipsis">
          {item.detail}
        </span>
      </div>

      <span className="text-xs text-color-secondary white-space-nowrap flex-shrink-0">
        {usedText(item.usedCount)}
      </span>
    </div>
  );

  return (
    <div className="flex flex-column">
      <div className="flex align-items-center justify-content-between flex-wrap gap-3 px-3 pt-3 pb-2">
        <span className="text-sm text-color-secondary">{note}</span>
        <Button
          type="button"
          label={addLabel}
          icon="pi pi-plus"
          size="small"
          outlined
          onClick={() => onSelect("new")}
        />
      </div>

      <ListBox
        value={typeof selected === "number" ? selected : null}
        options={items}
        optionValue="id"
        optionLabel="name"
        itemTemplate={itemTemplate}
        emptyMessage={emptyText}
        // Clicking the selected row again would clear it, and the panel with it.
        onChange={(e) => {
          if (e.value != null) onSelect(e.value as number);
        }}
        className="w-full border-none bg-transparent px-2"
      />
    </div>
  );
}

const usedText = (count: number) => {
  if (count === 0) return "unused";
  if (count === 1) return "1 step";
  return `${count} steps`;
};
