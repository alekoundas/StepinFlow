import { Button } from "primereact/button";
import { ListBox } from "primereact/listbox";

import IconComponent from "@/shared/components/IconComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowDto } from "@/shared/models/database/flow-dto";
import type { FlowValidationIssueDto } from "@/shared/models/database/flow-validation-result-dto";
import { flowHealthOf, severityLookOf, severityRank } from "@/features/flow/hooks/use-flow-validation";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";

export type WorkflowPartTab = "areas" | "locations" | "inputs";

interface Props {
  flowId: number;
  flow: FlowDto;
  issues: FlowValidationIssueDto[];
  onOpenPart: (tab: WorkflowPartTab, id: number) => void;
}

// One row for each step, however many issues it has, and one for each issue with the flow itself.
interface IssueRow {
  key: string;
  flowStepId: number | null;
  name: string;
  path: string[];
  parentFlowStepIds: number[];
  issues: FlowValidationIssueDto[];
}

/**
 * The left side of the Issues tab. Picking a row opens its step's form on the right, so the list
 * stays while each one is fixed, and a fixed one leaves it. Show in tree opens the step where it
 * sits, for an issue that is about where that is.
 */
export function WorkflowIssueListComponent({ flowId, flow, issues, onOpenPart }: Props) {
  const { selectedTreeNode, setSelectedTreeNode, setSelectedFlowStepTypeToAdd, setStepToReveal } =
    useWorkflowStore();

  const rows = rowsOf(issues);

  let selectedKey: string | null = null;
  if (selectedTreeNode && !selectedTreeNode.isFlow && !selectedTreeNode.isNew)
    selectedKey = `step-${selectedTreeNode.entityId}`;

  // The step's row from the tree, as the form needs it, read from its parent's children.
  const openStep = async (row: IssueRow) => {
    const parentId = row.parentFlowStepIds.length > 0 ? row.parentFlowStepIds[row.parentFlowStepIds.length - 1] : null;

    const siblings = await backendApiService.FlowStep.getTreeNodes({
      id: parentId ?? flowId,
      isFlow: parentId === null,
    });

    const node = siblings.find((x) => x.entityId === row.flowStepId);
    if (!node) return;

    setSelectedFlowStepTypeToAdd(undefined);
    setSelectedTreeNode(node);
  };

  // A name two parts share opens the first of them; anything else about the flow opens its form.
  const openFlowIssue = async (issue: FlowValidationIssueDto) => {
    const part = partNamed(flow, issue.flowStepName);
    if (part) {
      onOpenPart(part.tab, part.id);
      return;
    }

    const [flowNode] = await backendApiService.Flow.getTreeNodes(flowId);
    if (!flowNode) return;

    setSelectedFlowStepTypeToAdd(undefined);
    setSelectedTreeNode(flowNode);
  };

  const open = (row: IssueRow) => {
    if (row.flowStepId === null) void openFlowIssue(row.issues[0]);
    else void openStep(row);
  };

  const itemTemplate = (row: IssueRow) => {
    const worst = severityLookOf(worstOf(row).severity);

    return (
      <div className="flex align-items-start gap-3 w-full min-w-0">
        <span
          className="flex align-items-center justify-content-center flex-shrink-0 w-2rem h-2rem border-round-sm"
          style={{ color: worst.color, background: "var(--surface-hover)" }}
        >
          <IconComponent
            name={worst.icon}
            size="sm"
          />
        </span>

        <div className="flex flex-column gap-1 flex-1 min-w-0">
          <span className="font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">{row.name}</span>

          {row.path.length > 0 && (
            <span className="text-xs text-color-secondary white-space-nowrap overflow-hidden text-overflow-ellipsis">
              {row.path.join(" › ")}
            </span>
          )}

          {row.issues.map((issue, index) => (
            <span
              key={`${issue.code}-${index}`}
              className="flex align-items-start gap-2 text-sm white-space-normal"
            >
              <span style={{ color: severityLookOf(issue.severity).color }}>
                <IconComponent
                  name={severityLookOf(issue.severity).icon}
                  size="xs"
                />
              </span>
              {issue.message}
            </span>
          ))}
        </div>

        {row.flowStepId !== null && (
          <Button
            type="button"
            label="Show in tree"
            icon="pi pi-sitemap"
            size="small"
            text
            className="flex-shrink-0 py-1 px-2"
            onClick={(e) => {
              // The row underneath would take the click as picking it.
              e.stopPropagation();
              setStepToReveal(row.flowStepId);
            }}
          />
        )}
      </div>
    );
  };

  return (
    <div className="flex flex-column">
      <span className="text-sm text-color-secondary px-3 pt-3 pb-2">
        {rows.length === 0
          ? "Nothing to fix: the flow is ready to execute."
          : `${flowHealthOf(issues).text}. Pick one to fix it on the right.`}
      </span>

      <ListBox
        value={selectedKey}
        options={rows}
        optionValue="key"
        optionLabel="name"
        itemTemplate={itemTemplate}
        emptyMessage="No issues"
        onChange={(e) => {
          const row = rows.find((x) => x.key === e.value);
          if (row) open(row);
        }}
        className="w-full border-none bg-transparent px-2"
      />
    </div>
  );
}

// The backend sends them in the tree's order, the flow's own first, and that order is kept within
// each kind: errors, then warnings, then notes.
function rowsOf(issues: FlowValidationIssueDto[]): IssueRow[] {
  const rows: IssueRow[] = [];
  const byStepId = new Map<number, IssueRow>();

  issues.forEach((issue, index) => {
    if (issue.flowStepId == null) {
      rows.push({
        key: `flow-${index}`,
        flowStepId: null,
        name: issue.flowStepName || "The flow",
        path: [],
        parentFlowStepIds: [],
        issues: [issue],
      });
      return;
    }

    let row = byStepId.get(issue.flowStepId);
    if (!row) {
      row = {
        key: `step-${issue.flowStepId}`,
        flowStepId: issue.flowStepId,
        name: issue.flowStepName,
        path: issue.path,
        parentFlowStepIds: issue.parentFlowStepIds,
        issues: [],
      };
      byStepId.set(issue.flowStepId, row);
      rows.push(row);
    }

    row.issues.push(issue);
  });

  return rows.sort((a, b) => severityRank(worstOf(a).severity) - severityRank(worstOf(b).severity));
}

function worstOf(row: IssueRow): FlowValidationIssueDto {
  return row.issues.reduce((worst, x) => (severityRank(x.severity) < severityRank(worst.severity) ? x : worst));
}

// Names are compared as the backend does: trimmed, and in any case.
function partNamed(flow: FlowDto, name: string): { tab: WorkflowPartTab; id: number } | null {
  const wanted = name.trim().toLowerCase();
  if (wanted === "") return null;

  const isNamed = (x: { name: string }) => x.name.trim().toLowerCase() === wanted;

  const area = flow.flowAreas.find(isNamed);
  if (area) return { tab: "areas", id: area.id };

  const point = flow.flowPoints.find(isNamed);
  if (point) return { tab: "locations", id: point.id };

  const input = flow.flowCsvColumns.find(isNamed);
  if (input) return { tab: "inputs", id: input.id };

  return null;
}
