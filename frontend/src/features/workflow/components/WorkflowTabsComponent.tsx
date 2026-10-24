import { TabMenu } from "primereact/tabmenu";
import type { MenuItem, MenuItemOptions } from "primereact/menuitem";

import { useWorkflowStore, type WorkflowTab } from "@/features/workflow/store/workflow-store";
import { WorkflowTreeZoomComponent } from "@/features/workflow/components/WorkflowTreeZoomComponent";

interface Props {
  areaCount?: number;
  locationCount?: number;
  inputCount?: number;
  templateCount?: number;
}

const TABS: { tab: WorkflowTab; label: string }[] = [
  { tab: "flow", label: "Flow" },
  { tab: "areas", label: "Areas" },
  { tab: "locations", label: "Locations" },
  { tab: "inputs", label: "Inputs" },
  { tab: "templates", label: "Templates" },
  { tab: "script", label: "Script" },
];

/** The editor's tabs, each part with how many the flow has. */
export function WorkflowTabsComponent({ areaCount, locationCount, inputCount, templateCount }: Props) {
  const { activeTab, setActiveTab } = useWorkflowStore();

  const counts: Partial<Record<WorkflowTab, number | undefined>> = {
    areas: areaCount,
    locations: locationCount,
    inputs: inputCount,
    templates: templateCount,
  };

  const model: MenuItem[] = TABS.map(({ tab, label }) => ({
    label,
    template: (_: MenuItem, options: MenuItemOptions) => (
      <a
        className={options.className}
        onClick={options.onClick}
        role="tab"
        tabIndex={0}
      >
        <span className={options.labelClassName}>{label}</span>
        {counts[tab] !== undefined && <span className="ml-2 text-color-secondary">{counts[tab]}</span>}
      </a>
    ),
  }));

  // The size control continues the tab bar's line, so it reads as part of the bar.
  return (
    <div className="flex px-3 pt-2">
      <TabMenu
        model={model}
        activeIndex={TABS.findIndex((x) => x.tab === activeTab)}
        onTabChange={(e) => setActiveTab(TABS[e.index].tab)}
        className="flex-1 min-w-0"
        pt={{ menu: { className: "bg-transparent" } }}
      />
      {activeTab === "flow" && (
        <div
          className="flex align-items-center pl-2"
          style={{ borderBottom: "2px solid var(--surface-d)" }}
        >
          <WorkflowTreeZoomComponent />
        </div>
      )}
    </div>
  );
}
