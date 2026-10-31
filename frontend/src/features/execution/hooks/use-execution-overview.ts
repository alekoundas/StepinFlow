import { useMemo } from "react";

import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { hasBranches } from "@/shared/models/flow-step-catalog";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

// A lone pass folded is one row in place of a dozen, but it hides the pass beside the ones that
// found something, which is the context they are read in.
const MIN_FOLDED_PASSES = 2;

const MAX_ACTIVITY_BARS = 120;

const BAR_SIZES_MILLISECONDS = [
  1_000, 2_000, 5_000, 10_000, 15_000, 30_000, 60_000, 120_000, 300_000, 600_000, 900_000,
  1_800_000, 3_600_000,
];

export interface StepCounts {
  executed: number;
  found: number;
  failed: number;
}

export interface ActivityBar {
  steps: number;
  found: number;
  hasFatal: boolean;
}

export interface Activity {
  barMilliseconds: number;
  bars: ActivityBar[];
}

export interface LoopProgress {
  name: string;

  /** From one, as the rows read it. */
  pass: number;
  passesThatFound: number;
  lastPassThatFound: number | undefined;
}

export interface ExecutionOverview {
  /** A loop pass that found nothing, by the index of the loop row it opens with: one past its last row. */
  quietPassEnds: Map<number, number>;
  countsByFlowStepId: Map<number, StepCounts>;
  activity: Activity;

  /** Epoch milliseconds. Undefined until a row has arrived. */
  startedAt: number | undefined;
  endedAt: number | undefined;

  foundCount: number;
  handledCount: number;
  fatalSequence: number | undefined;

  /** The outermost loop: the one a long execution goes round. */
  loop: LoopProgress | undefined;
}

export type ExecutionListItem =
  | { kind: "step"; step: ExecutionStepDto }
  | {
      kind: "fold";
      firstSequence: number;
      lastSequence: number;
      passes: number;
      firstPass: number;
      lastPass: number;
      stepCount: number;
      isOpen: boolean;
    };

export type ExecutionListFilter = "all" | "found" | "failed";

/** Everything the page draws besides the rows, worked out in one walk over them. */
export function useExecutionOverview(
  executionSteps: ExecutionStepDto[],
  errorFlowStepId: number | null | undefined,
  isLive: boolean,
): ExecutionOverview {
  return useMemo(
    () => buildExecutionOverview(executionSteps, errorFlowStepId, isLive),
    [executionSteps, errorFlowStepId, isLive],
  );
}

/**
 * A pass is quiet when it ran at least one check and none of them succeeded: the passes a bot loop
 * spends looking and finding nothing, which are most of an execution left on for hours.
 *
 * The pass still executing is never quiet, it may yet find something; nor the one that ended the
 * execution.
 */
export function buildExecutionOverview(
  executionSteps: ExecutionStepDto[],
  errorFlowStepId: number | null | undefined,
  isLive: boolean,
): ExecutionOverview {
  const count = executionSteps.length;
  const fatalIndex = indexOfFatal(executionSteps, errorFlowStepId);

  // How many checks, and found, before each row, so a pass's are two subtractions.
  const checksBefore = new Int32Array(count + 1);
  const foundBefore = new Int32Array(count + 1);
  const countsByFlowStepId = new Map<number, StepCounts>();
  let handledCount = 0;

  for (let i = 0; i < count; i++) {
    const step = executionSteps[i];
    const isCheck = isCheckStep(step);
    const isFound = isCheck && step.outcome === StepOutcomeEnum.SUCCESS;

    checksBefore[i + 1] = checksBefore[i] + (isCheck ? 1 : 0);
    foundBefore[i + 1] = foundBefore[i] + (isFound ? 1 : 0);

    if (step.outcome === StepOutcomeEnum.FAILURE && i !== fatalIndex) handledCount++;

    if (step.flowStepId && !isServedHit(step)) {
      const counts = countsByFlowStepId.get(step.flowStepId) ?? { executed: 0, found: 0, failed: 0 };
      counts.executed++;
      if (isFound) counts.found++;
      if (step.outcome === StepOutcomeEnum.FAILURE) counts.failed++;
      countsByFlowStepId.set(step.flowStepId, counts);
    }
  }

  // A pass is its loop row and every deeper row after it.
  const passes: Pass[] = [];
  const open: number[] = [];
  for (let i = 0; i < count; i++) {
    const depth = executionSteps[i].depth;
    while (open.length > 0 && executionSteps[open[open.length - 1]].depth >= depth)
      passes.push({ index: open.pop()!, end: i, isComplete: true });

    if (executionSteps[i].flowStepType === FlowStepTypeEnum.LOOP) open.push(i);
  }
  while (open.length > 0) passes.push({ index: open.pop()!, end: count, isComplete: !isLive });

  const quietPassEnds = new Map<number, number>();
  for (const pass of passes) {
    const checks = checksBefore[pass.end] - checksBefore[pass.index + 1];
    const found = foundBefore[pass.end] - foundBefore[pass.index + 1];
    const holdsFatal = fatalIndex >= pass.index && fatalIndex < pass.end;

    if (pass.isComplete && checks > 0 && found === 0 && !holdsFatal)
      quietPassEnds.set(pass.index, pass.end);
  }

  const startedAt = count > 0 ? startedAtOf(executionSteps[0]) : undefined;
  const last = count > 0 ? executionSteps[count - 1] : undefined;
  const endedAt = last ? startedAtOf(last) + last.durationMilliseconds : undefined;

  return {
    quietPassEnds,
    countsByFlowStepId,
    activity: buildActivity(executionSteps, fatalIndex, startedAt, endedAt),
    startedAt,
    endedAt,
    foundCount: foundBefore[count],
    handledCount,
    fatalSequence: fatalIndex >= 0 ? executionSteps[fatalIndex].sequence : undefined,
    loop: buildLoopProgress(executionSteps, passes, foundBefore),
  };
}

/**
 * The rows newest first, each run of quiet passes of one loop as a single row. An open fold keeps
 * its row, above the passes it holds.
 */
export function foldQuietPasses(
  executionSteps: ExecutionStepDto[],
  quietPassEnds: Map<number, number>,
  openFolds: ReadonlySet<number>,
): ExecutionListItem[] {
  const items: ExecutionListItem[] = [];

  let i = 0;
  while (i < executionSteps.length) {
    const first = executionSteps[i];
    let end = quietPassEnds.get(i);

    if (end !== undefined) {
      let passes = 1;
      let lastPassIndex = i;

      for (
        let next = quietPassEnds.get(end);
        next !== undefined && isSameLoop(first, executionSteps[end]);
        next = quietPassEnds.get(end)
      ) {
        lastPassIndex = end;
        end = next;
        passes++;
      }

      if (passes >= MIN_FOLDED_PASSES) {
        const isOpen = openFolds.has(first.sequence);
        if (isOpen)
          for (let k = i; k < end; k++) items.push({ kind: "step", step: executionSteps[k] });

        items.push({
          kind: "fold",
          firstSequence: first.sequence,
          lastSequence: executionSteps[end - 1].sequence,
          passes,
          firstPass: (first.loopPass ?? 0) + 1,
          lastPass: (executionSteps[lastPassIndex].loopPass ?? 0) + 1,
          stepCount: end - i,
          isOpen,
        });

        i = end;
        continue;
      }
    }

    items.push({ kind: "step", step: first });
    i++;
  }

  return items.reverse();
}

/** The rows a filter or a search keeps, newest first. Nothing folds: what is asked for is shown. */
export function filterSteps(
  executionSteps: ExecutionStepDto[],
  filter: ExecutionListFilter,
  query: string,
): ExecutionListItem[] {
  const text = query.trim().toLowerCase();
  const sequence = /^\d+$/.test(text) ? Number(text) : undefined;

  const items: ExecutionListItem[] = [];
  for (let i = executionSteps.length - 1; i >= 0; i--) {
    const step = executionSteps[i];

    if (filter === "found" && !(isCheckStep(step) && step.outcome === StepOutcomeEnum.SUCCESS))
      continue;
    if (filter === "failed" && step.outcome !== StepOutcomeEnum.FAILURE) continue;
    if (text && step.sequence !== sequence && !step.name.toLowerCase().includes(text)) continue;

    items.push({ kind: "step", step });
  }

  return items;
}

/** A row by its sequence. Sequences count up from zero, so it is nearly always at its own number. */
export function findStep(
  executionSteps: ExecutionStepDto[],
  sequence: number,
): ExecutionStepDto | undefined {
  const guess = executionSteps[sequence];
  if (guess?.sequence === sequence) return guess;

  return executionSteps.find((x) => x.sequence === sequence);
}

/** A step that routes on its outcome: it either found what it looked for or went to its Failure. */
export function isCheckStep(step: ExecutionStepDto): boolean {
  return hasBranches(step.flowStepType) && !isServedHit(step);
}

/** A hit handed out from an earlier search's screenshot. Nothing executed, so it carries no time. */
export function isServedHit(step: ExecutionStepDto): boolean {
  return step.matchIndex !== null && step.matchIndex !== undefined && step.matchIndex > 0;
}

// Parsing every row's date on every batch would cost more than everything else here together.
const startedAtCache = new WeakMap<ExecutionStepDto, number>();

export function startedAtOf(step: ExecutionStepDto): number {
  let startedAt = startedAtCache.get(step);
  if (startedAt === undefined) {
    startedAt = Date.parse(step.startedOn);
    startedAtCache.set(step, startedAt);
  }

  return startedAt;
}

// The step that ended it fails last: the execution stopped there.
function indexOfFatal(
  executionSteps: ExecutionStepDto[],
  errorFlowStepId: number | null | undefined,
): number {
  if (!errorFlowStepId) return -1;

  for (let i = executionSteps.length - 1; i >= 0; i--) {
    const step = executionSteps[i];
    if (step.flowStepId === errorFlowStepId && step.outcome === StepOutcomeEnum.FAILURE) return i;
  }

  return -1;
}

function isSameLoop(first: ExecutionStepDto, other: ExecutionStepDto): boolean {
  if (other.flowStepType !== FlowStepTypeEnum.LOOP || other.depth !== first.depth) return false;

  // Steps deleted since keep their rows with no step to point at.
  if (first.flowStepId) return other.flowStepId === first.flowStepId;
  return other.name === first.name;
}

function buildActivity(
  executionSteps: ExecutionStepDto[],
  fatalIndex: number,
  startedAt: number | undefined,
  endedAt: number | undefined,
): Activity {
  if (startedAt === undefined || endedAt === undefined || Number.isNaN(startedAt + endedAt))
    return { barMilliseconds: BAR_SIZES_MILLISECONDS[0], bars: [] };

  const span = Math.max(1, endedAt - startedAt);
  const barMilliseconds =
    BAR_SIZES_MILLISECONDS.find((x) => Math.ceil(span / x) <= MAX_ACTIVITY_BARS) ??
    Math.ceil(span / MAX_ACTIVITY_BARS / 3_600_000) * 3_600_000;

  const bars: ActivityBar[] = Array.from({ length: Math.ceil(span / barMilliseconds) }, () => ({
    steps: 0,
    found: 0,
    hasFatal: false,
  }));

  for (let i = 0; i < executionSteps.length; i++) {
    const step = executionSteps[i];
    const at = startedAtOf(step);
    if (Number.isNaN(at)) continue;

    const bar = bars[Math.min(bars.length - 1, Math.max(0, Math.floor((at - startedAt) / barMilliseconds)))];
    bar.steps++;
    if (isCheckStep(step) && step.outcome === StepOutcomeEnum.SUCCESS) bar.found++;
    if (i === fatalIndex) bar.hasFatal = true;
  }

  return { barMilliseconds, bars };
}

function buildLoopProgress(
  executionSteps: ExecutionStepDto[],
  passes: Pass[],
  foundBefore: Int32Array,
): LoopProgress | undefined {
  if (passes.length === 0) return undefined;

  let outermost = passes[0];
  for (const pass of passes) {
    const depth = executionSteps[pass.index].depth;
    const outermostDepth = executionSteps[outermost.index].depth;

    if (depth < outermostDepth || (depth === outermostDepth && pass.index > outermost.index))
      outermost = pass;
  }

  const loop = executionSteps[outermost.index];
  let passesThatFound = 0;
  let lastPassThatFound: number | undefined;

  for (const pass of passes) {
    const row = executionSteps[pass.index];
    if (!isSameLoop(loop, row)) continue;
    if (foundBefore[pass.end] - foundBefore[pass.index + 1] === 0) continue;

    passesThatFound++;
    lastPassThatFound = Math.max(lastPassThatFound ?? 0, (row.loopPass ?? 0) + 1);
  }

  return {
    name: loop.name,
    pass: (loop.loopPass ?? 0) + 1,
    passesThatFound,
    lastPassThatFound,
  };
}

interface Pass {
  /** The loop row it opens with. */
  index: number;

  /** One past its last row. */
  end: number;

  /** Something came after it, or the execution is over. Until then it may still find something. */
  isComplete: boolean;
}
