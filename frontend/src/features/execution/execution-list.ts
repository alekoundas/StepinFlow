import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { isCheckStep, isSameLoop } from "@/features/execution/execution-overview";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

// A lone pass folded is one row in place of a dozen, but it hides the pass beside the ones that
// found something, which is the context they are read in.
const MIN_FOLDED_PASSES = 2;

/** Loop passes that found nothing, kept as a count once their rows were let go. */
export interface QuietRun {
  firstSequence: number;
  lastSequence: number;
  passes: number;
  firstPass: number;
  lastPass: number;
  stepCount: number;

  /** The loop row of its first pass, which a pass after it is matched against. */
  loop: ExecutionStepDto;
}

/** The oldest rows, let go of whole once even the passes that found something were too many to keep. */
export interface ReleasedRows {
  lastSequence: number;
  stepCount: number;
}

/**
 * What a live execution's page holds: the rows, oldest first, and counts in place of the ones let
 * go of. A past execution keeps every row.
 */
export interface KeptRows {
  executionSteps: ExecutionStepDto[];
  quietRuns: QuietRun[];
  released: ReleasedRows | undefined;
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

      /** Its rows up to here were let go of, and are read back from the history to be shown. */
      releasedThrough: number | undefined;
    }
  | { kind: "released"; stepCount: number };

export type ExecutionListFilter = "all" | "found" | "failed";

/**
 * Lets go of the rows of loop passes that found nothing, older than the newest rows, keeping a
 * count of them in their place. They are most of an execution left on for hours, they are what a
 * fold hides anyway, and the history has them.
 *
 * Only passes of a loop no other loop holds, and only ones another quiet pass of that loop sits
 * beside, so what is let go of is always inside a fold.
 */
export function releaseQuietPasses(kept: KeptRows, keepNewest: number): ReleasedPasses {
  const rows = kept.executionSteps;
  const cut = rows.length - keepNewest;
  if (cut <= 0) return { kept, rowsByRun: new Map() };

  const passes = findPasses(rows, true, undefined)
    .filter((x) => x.isOutermost)
    .sort((a, b) => a.index - b.index);

  const runs = kept.quietRuns.map((x) => ({ ...x }));
  const runByNextSequence = new Map(runs.map((x) => [x.lastSequence + 1, x]));
  const isBeside = (earlier: Pass, later: Pass) =>
    earlier.end === later.index && follows(rows, later.index) && isSameLoop(rows[earlier.index], rows[later.index]);

  const released: Pass[] = [];
  for (let p = 0; p < passes.length; p++) {
    const pass = passes[p];
    if (pass.end > cut) break;
    if (!pass.isQuiet) continue;

    const previous = passes[p - 1];
    const next = passes[p + 1];
    const run = runByNextSequence.get(rows[pass.index].sequence);

    if (
      (previous?.isQuiet && isBeside(previous, pass)) ||
      (next?.isQuiet && isBeside(pass, next)) ||
      (run && isSameLoop(run.loop, rows[pass.index]))
    )
      released.push(pass);
  }

  if (released.length === 0) return { kept, rowsByRun: new Map() };

  const rowsByRun = new Map<QuietRun, ExecutionStepDto[]>();
  const isReleased = new Uint8Array(rows.length);

  for (const pass of released) {
    const loop = rows[pass.index];
    const passRows = rows.slice(pass.index, pass.end);
    isReleased.fill(1, pass.index, pass.end);

    let run = runByNextSequence.get(loop.sequence);
    if (run && !isSameLoop(run.loop, loop)) run = undefined;

    if (run) {
      runByNextSequence.delete(run.lastSequence + 1);
      run.passes++;
      run.lastPass = (loop.loopPass ?? 0) + 1;
      run.stepCount += passRows.length;
    } else {
      run = {
        firstSequence: loop.sequence,
        lastSequence: loop.sequence,
        passes: 1,
        firstPass: (loop.loopPass ?? 0) + 1,
        lastPass: (loop.loopPass ?? 0) + 1,
        stepCount: passRows.length,
        loop,
      };
      runs.push(run);
    }

    run.lastSequence = passRows[passRows.length - 1].sequence;
    runByNextSequence.set(run.lastSequence + 1, run);

    const runRows = rowsByRun.get(run) ?? [];
    runRows.push(...passRows);
    rowsByRun.set(run, runRows);
  }

  return {
    kept: {
      executionSteps: rows.filter((_, i) => !isReleased[i]),
      quietRuns: runs.sort((a, b) => a.firstSequence - b.firstSequence),
      released: kept.released,
    },
    rowsByRun,
  };
}

/**
 * Lets go of the oldest rows once there are more than the most a page keeps, down to a little
 * under it, so this does not come round again on the next batch. Cut where a pass starts when one
 * is near, so the oldest row left is not halfway through one.
 */
export function releaseOldestRows(kept: KeptRows, maxKept: number): KeptRows {
  const rows = kept.executionSteps;
  if (rows.length <= maxKept) return kept;

  const least = rows.length - Math.floor(maxKept * 0.9);
  const passStart = findPasses(rows, true, undefined)
    .filter((x) => x.isOutermost && x.index >= least && x.index < least + maxKept / 10)
    .reduce<number | undefined>((first, x) => Math.min(first ?? x.index, x.index), undefined);
  const cut = passStart ?? least;

  const firstKept = rows[cut].sequence;
  const releasedRuns = kept.quietRuns.filter((x) => x.lastSequence < firstKept);

  return {
    executionSteps: rows.slice(cut),
    quietRuns: kept.quietRuns.filter((x) => x.lastSequence >= firstKept),
    released: {
      lastSequence: firstKept - 1,
      stepCount:
        (kept.released?.stepCount ?? 0) + cut + releasedRuns.reduce((sum, x) => sum + x.stepCount, 0),
    },
  };
}

/**
 * The rows newest first, each run of quiet passes of one loop as a single row. An open fold keeps
 * its row, above the passes it holds.
 */
export function foldQuietPasses(
  kept: KeptRows,
  openFolds: ReadonlySet<number>,
  foldRows: ReadonlyMap<number, ExecutionStepDto[]>,
  isLive: boolean,
  fatalSequence: number | undefined,
): ExecutionListItem[] {
  const rows = kept.executionSteps;
  const runs = kept.quietRuns;

  const quietPassEnds = new Map<number, number>();
  for (const pass of findPasses(rows, isLive, fatalSequence))
    if (pass.isQuiet) quietPassEnds.set(pass.index, pass.end);

  // From the pass at start, the passes of loop that follow it straight on and found nothing.
  const chainFrom = (start: number, loop: ExecutionStepDto) => {
    let end = quietPassEnds.get(start)!;
    let passes = 1;
    let lastPassIndex = start;

    for (
      let next = quietPassEnds.get(end);
      next !== undefined && isSameLoop(loop, rows[end]) && follows(rows, end);
      next = quietPassEnds.get(end)
    ) {
      lastPassIndex = end;
      end = next;
      passes++;
    }

    return { end, passes, lastPassIndex };
  };

  const pushRows = (start: number, end: number, items: ExecutionListItem[]) => {
    for (let k = start; k < end; k++) items.push({ kind: "step", step: rows[k] });
  };

  const items: ExecutionListItem[] = [];
  if (kept.released) items.push({ kind: "released", stepCount: kept.released.stepCount });

  let r = 0;
  let i = 0;
  while (i < rows.length || r < runs.length) {
    // A run let go of, and the quiet passes kept that carry straight on from it, are one fold.
    if (r < runs.length && (i >= rows.length || runs[r].lastSequence < rows[i].sequence)) {
      const run = runs[r++];
      const carriesOn =
        i < rows.length &&
        quietPassEnds.has(i) &&
        rows[i].sequence === run.lastSequence + 1 &&
        isSameLoop(run.loop, rows[i]);
      const chain = carriesOn ? chainFrom(i, run.loop) : { end: i, passes: 0, lastPassIndex: i };

      const isOpen = openFolds.has(run.firstSequence);
      if (isOpen) {
        for (const step of foldRows.get(run.firstSequence) ?? []) items.push({ kind: "step", step });
        pushRows(i, chain.end, items);
      }

      items.push({
        kind: "fold",
        firstSequence: run.firstSequence,
        lastSequence: chain.end > i ? rows[chain.end - 1].sequence : run.lastSequence,
        passes: run.passes + chain.passes,
        firstPass: run.firstPass,
        lastPass: chain.passes > 0 ? (rows[chain.lastPassIndex].loopPass ?? 0) + 1 : run.lastPass,
        stepCount: run.stepCount + chain.end - i,
        isOpen,
        releasedThrough: run.lastSequence,
      });

      i = chain.end;
      continue;
    }

    const first = rows[i];
    if (quietPassEnds.has(i)) {
      const chain = chainFrom(i, first);

      if (chain.passes >= MIN_FOLDED_PASSES) {
        const isOpen = openFolds.has(first.sequence);
        if (isOpen) pushRows(i, chain.end, items);

        items.push({
          kind: "fold",
          firstSequence: first.sequence,
          lastSequence: rows[chain.end - 1].sequence,
          passes: chain.passes,
          firstPass: (first.loopPass ?? 0) + 1,
          lastPass: (rows[chain.lastPassIndex].loopPass ?? 0) + 1,
          stepCount: chain.end - i,
          isOpen,
          releasedThrough: undefined,
        });

        i = chain.end;
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

/**
 * A row by its sequence. Sequences count up from zero, so with every row kept it is at its own
 * number; with some let go of, the rows are still in order.
 */
export function findStep(
  executionSteps: ExecutionStepDto[],
  sequence: number,
): ExecutionStepDto | undefined {
  const guess = executionSteps[sequence];
  if (guess?.sequence === sequence) return guess;

  let low = 0;
  let high = executionSteps.length - 1;
  while (low <= high) {
    const middle = (low + high) >> 1;
    const step = executionSteps[middle];

    if (step.sequence === sequence) return step;
    if (step.sequence < sequence) low = middle + 1;
    else high = middle - 1;
  }

  return undefined;
}

// Nothing let go of between this row and the one before it.
function follows(rows: ExecutionStepDto[], index: number): boolean {
  return index > 0 && index < rows.length && rows[index].sequence === rows[index - 1].sequence + 1;
}

/**
 * A pass is quiet when it ran at least one check and none of them succeeded: the passes a bot loop
 * spends looking and finding nothing.
 *
 * The pass still executing is never quiet, it may yet find something; nor the one that ended the
 * execution.
 */
function findPasses(
  rows: ExecutionStepDto[],
  isLive: boolean,
  fatalSequence: number | undefined,
): Pass[] {
  const count = rows.length;
  const fatal = fatalSequence === undefined ? undefined : findStep(rows, fatalSequence);
  const fatalIndex = fatal ? rows.indexOf(fatal) : -1;

  // How many checks, and found, before each row, so a pass's are two subtractions.
  const checksBefore = new Int32Array(count + 1);
  const foundBefore = new Int32Array(count + 1);
  for (let i = 0; i < count; i++) {
    const isCheck = isCheckStep(rows[i]);
    checksBefore[i + 1] = checksBefore[i] + (isCheck ? 1 : 0);
    foundBefore[i + 1] = foundBefore[i] + (isCheck && rows[i].outcome === StepOutcomeEnum.SUCCESS ? 1 : 0);
  }

  const passes: Pass[] = [];
  const open: number[] = [];
  const close = (end: number, isComplete: boolean) => {
    const index = open.pop()!;
    const checks = checksBefore[end] - checksBefore[index + 1];
    const found = foundBefore[end] - foundBefore[index + 1];
    const holdsFatal = fatalIndex >= index && fatalIndex < end;

    passes.push({
      index,
      end,
      isQuiet: isComplete && checks > 0 && found === 0 && !holdsFatal,
      isOutermost: open.length === 0,
    });
  };

  for (let i = 0; i < count; i++) {
    while (open.length > 0 && rows[open[open.length - 1]].depth >= rows[i].depth) close(i, true);
    if (rows[i].flowStepType === FlowStepTypeEnum.LOOP) open.push(i);
  }
  while (open.length > 0) close(count, !isLive);

  return passes;
}

interface Pass {
  /** The loop row it opens with. */
  index: number;

  /** One past its last row. */
  end: number;

  isQuiet: boolean;

  /** No other loop's pass holds it. */
  isOutermost: boolean;
}

interface ReleasedPasses {
  kept: KeptRows;

  /** The rows each run took in, for a fold already open on it. */
  rowsByRun: Map<QuietRun, ExecutionStepDto[]>;
}
