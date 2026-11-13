import { memo, useEffect, useEffectEvent, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { TreeExpandedKeysType } from "primereact/tree";
import { classNames } from "primereact/utils";

import ReadOnlyFlowTreeComponent from "@/shared/components/data-tree/ReadOnlyFlowTreeComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import {
  flowStepAccent,
  getFlowStepCatalogEntry,
  hasBranches,
  tint,
} from "@/shared/models/flow-step-catalog";
import { FOUND_COLOR, HANDLED_COLOR, MONO, MUTED } from "@/features/execution/execution-format";
import { useExecutionStore } from "@/features/execution/store/execution-store";
import { useExecutionMutations } from "@/features/execution/hooks/use-execution";
import type { StepCounts } from "@/features/execution/execution-overview";
import { keysUnderDisabled, type TreeNodeDto } from "@/shared/models/tree-node-dto";

interface Props {
  flowId: number;

  /** How often each step executed in the execution on the page, by flow step. */
  countsByFlowStepId: Map<number, StepCounts>;
}

/**
 * The flow, with a gutter you click to set a breakpoint and how often each step executed.
 *
 * Reuses the read-only tree, so a step sits where it does everywhere else, and only adds the
 * gutter, the counts and the "you are here" ring on top of it. The ring is followed: the branch it
 * is in opens and the panel scrolls to it, unless the pointer is over the panel.
 */
function ExecutionFlowTreeComponent({ flowId, countsByFlowStepId }: Props) {
  const breakpointStepIds = useExecutionStore((x) => x.breakpointStepIds);
  const currentFlowStepId = useExecutionStore((x) => x.currentFlowStepId);
  const runState = useExecutionStore((x) => x.runState);
  const toggleBreakpoint = useExecutionStore((x) => x.toggleBreakpoint);

  const { setBreakpointsMutation } = useExecutionMutations();

  // The whole flow at once, not a level at a time: a step you have not expanded to is a step you
  // could not put a breakpoint on.
  const { data: treeNodes } = useQuery({
    queryKey: ["flowStep", "treeNodesRecursive", flowId],
    queryFn: () => backendApiService.FlowStep.getTreeNodesRecursive(flowId),
    enabled: flowId > 0,
  });

  // A breakpoint added mid execution has to reach the engine, or it only takes effect next time.
  useEffect(() => {
    if (runState === RunStateEnum.FINISHED) return;

    setBreakpointsMutation.mutate(breakpointStepIds);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [breakpointStepIds]);

  // What you closed rather than what is open, so everything else is: a flow you are debugging is
  // one you want to see all of. Handed back through onToggle, since without it PrimeReact keeps
  // its own copy from the first render, which usually came before the tree did.
  const [collapsedKeys, setCollapsedKeys] = useState<ReadonlySet<string>>(() => new Set());

  const tree = useMemo(() => indexTree(treeNodes ?? []), [treeNodes]);

  const expandedKeys = useMemo(() => {
    const keys: TreeExpandedKeysType = {};
    for (const key of tree.keys) if (!collapsedKeys.has(key)) keys[key] = true;
    return keys;
  }, [tree, collapsedKeys]);

  const underDisabled = useMemo(() => keysUnderDisabled(treeNodes ?? []), [treeNodes]);

  const scrollerRef = useRef<HTMLDivElement>(null);
  const currentRowRef = useRef<HTMLDivElement>(null);
  const isPointerOverRef = useRef(false);

  // Not while the pointer is over the panel: the rows would move out from under it.
  const follow = useEffectEvent(() => {
    if (currentFlowStepId === undefined || isPointerOverRef.current) return;

    const closed = ancestorsOf(tree, currentFlowStepId).filter((key) => collapsedKeys.has(key));
    if (closed.length > 0) {
      // Scrolled to on the next pass, once its row is drawn.
      setCollapsedKeys((prev) => new Set([...prev].filter((key) => !closed.includes(key))));
      return;
    }

    centerIfOutOfView(currentRowRef.current, scrollerRef.current);
  });

  useEffect(() => {
    follow();
  }, [currentFlowStepId, collapsedKeys, treeNodes]);

  const ringColor = runState === RunStateEnum.PAUSED ? HANDLED_COLOR : FOUND_COLOR;

  return (
    // Its own scroll rather than the panel's, so the pointer on the scrollbar counts as over it.
    <div
      ref={scrollerRef}
      className="flex-1 min-h-0 overflow-auto px-2 py-1"
      onPointerEnter={() => {
        isPointerOverRef.current = true;
      }}
      onPointerLeave={() => {
        isPointerOverRef.current = false;
      }}
    >
      <ReadOnlyFlowTreeComponent
        value={treeNodes ?? []}
        expandedKeys={expandedKeys}
        onToggle={(keys) => setCollapsedKeys(new Set(tree.keys.filter((key) => !keys[key])))}
        nodeTemplate={(node: TreeNodeDto) => {
          const isBranch =
            node.flowStepType === FlowStepTypeEnum.SUCCESS ||
            node.flowStepType === FlowStepTypeEnum.FAILURE;
          const isCurrent = node.entityId === currentFlowStepId;

          return (
            <div
              ref={isCurrent ? currentRowRef : undefined}
              className={classNames("flex align-items-center gap-2 w-full min-w-0 px-2 border-round-lg", {
                "opacity-60": node.isDisabled || underDisabled.has(node.key),
              })}
              style={{
                height: 28,
                background: isCurrent ? tint(ringColor, 12) : undefined,
                boxShadow: isCurrent ? `inset 0 0 0 1px ${tint(ringColor, 55)}` : undefined,
              }}
            >
              {/* The engine never parks on a branch, so it has no gutter. */}
              {isBranch ? (
                <span style={{ width: 11, flex: "none" }} />
              ) : (
                <BreakpointDot
                  isOn={breakpointStepIds.includes(node.entityId)}
                  onToggle={() => toggleBreakpoint(node.entityId)}
                />
              )}

              {isBranch ? (
                <span
                  className="text-xs font-semibold"
                  style={{ color: flowStepAccent(node.flowStepType) }}
                >
                  {node.name}
                </span>
              ) : (
                <>
                  <StepGlyph flowStepType={node.flowStepType} />
                  <span
                    className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis min-w-0"
                    style={{ color: isCurrent ? ringColor : undefined }}
                  >
                    {node.name}
                  </span>
                </>
              )}

              <StepCount
                node={node}
                isBranch={isBranch}
                countsByFlowStepId={countsByFlowStepId}
              />
            </div>
          );
        }}
      />
    </div>
  );
}

// Drawn again when the step being executed, a breakpoint or a count changes.
export default memo(ExecutionFlowTreeComponent);

interface TreeIndex {
  keys: string[];
  keyByStepId: Map<number, string>;
  parentKeyByKey: Map<string, string>;
}

function indexTree(
  nodes: TreeNodeDto[],
  parentKey?: string,
  index: TreeIndex = { keys: [], keyByStepId: new Map(), parentKeyByKey: new Map() },
): TreeIndex {
  for (const node of nodes) {
    index.keys.push(node.key);
    index.keyByStepId.set(node.entityId, node.key);
    if (parentKey !== undefined) index.parentKeyByKey.set(node.key, parentKey);

    indexTree(node.children ?? [], node.key, index);
  }

  return index;
}

/** The rows above a step. None for a step this flow does not have, such as a sub-flow's. */
function ancestorsOf(tree: TreeIndex, flowStepId: number): string[] {
  const keys: string[] = [];

  const stepKey = tree.keyByStepId.get(flowStepId);
  if (stepKey === undefined) return keys;

  for (let key = tree.parentKeyByKey.get(stepKey); key !== undefined; key = tree.parentKeyByKey.get(key))
    keys.push(key);

  return keys;
}

// Only when it is out of view, and to the middle, so the steps after it are in view too.
function centerIfOutOfView(row: HTMLElement | null, scroller: HTMLElement | null): void {
  if (!row || !scroller) return;

  const rowBox = row.getBoundingClientRect();
  const scrollerBox = scroller.getBoundingClientRect();
  if (rowBox.top >= scrollerBox.top && rowBox.bottom <= scrollerBox.bottom) return;

  scroller.scrollTop += rowBox.top - scrollerBox.top - (scroller.clientHeight - rowBox.height) / 2;
}

interface StepGlyphProps {
  flowStepType?: FlowStepTypeEnum;
}

function StepGlyph({ flowStepType }: StepGlyphProps) {
  const accent = flowStepAccent(flowStepType);

  return (
    <span
      className="flex align-items-center justify-content-center border-round-sm"
      style={{ width: 20, height: 20, flex: "none", color: accent, background: tint(accent, 18) }}
    >
      <i
        className={`pi pi-${getFlowStepCatalogEntry(flowStepType)?.iconName ?? "circle"}`}
        style={{ fontSize: "0.65rem" }}
      />
    </span>
  );
}

interface StepCountProps {
  node: TreeNodeDto;
  isBranch: boolean;
  countsByFlowStepId: Map<number, StepCounts>;
}

/** A check says how often it found; a branch how often it was taken; anything else how often it executed. */
function StepCount({ node, isBranch, countsByFlowStepId }: StepCountProps) {
  const counts = countsByFlowStepId.get(isBranch ? (node.parentFlowStepId ?? 0) : node.entityId);
  if (!counts) return null;

  let text = `×${counts.executed.toLocaleString()}`;
  let title = `Executed ${counts.executed.toLocaleString()} times`;
  let color = MUTED.color;

  if (isBranch) {
    const taken = node.flowStepType === FlowStepTypeEnum.SUCCESS ? counts.found : counts.failed;
    if (taken === 0) return null;

    text = `×${taken.toLocaleString()}`;
    title = `Taken ${taken.toLocaleString()} times`;
  } else if (hasBranches(node.flowStepType) && counts.found > 0) {
    text = `found ${counts.found.toLocaleString()}`;
    title += `, found ${counts.found.toLocaleString()}`;
    color = FOUND_COLOR;
  }

  return (
    <span
      className="ml-auto pl-2 text-xs white-space-nowrap"
      style={{ fontFamily: MONO, color }}
      title={title}
    >
      {text}
    </span>
  );
}

interface BreakpointDotProps {
  isOn: boolean;
  onToggle: () => void;
}

/** An empty ring until you click it, so the whole gutter column is a target. */
function BreakpointDot({ isOn, onToggle }: BreakpointDotProps) {
  const [isHovered, setIsHovered] = useState(false);

  return (
    <i
      role="button"
      aria-pressed={isOn}
      aria-label={isOn ? "Remove the breakpoint" : "Set a breakpoint"}
      onClick={(event) => {
        event.stopPropagation();
        onToggle();
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{
        width: 11,
        height: 11,
        borderRadius: "50%",
        flex: "0 0 auto",
        cursor: "pointer",
        background: isOn ? "var(--red-500)" : undefined,
        border: `1px solid ${
          isOn || isHovered ? "var(--red-500)" : "var(--surface-border)"
        }`,
      }}
    />
  );
}
