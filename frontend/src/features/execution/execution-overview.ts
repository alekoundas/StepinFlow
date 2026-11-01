import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";
import { hasBranches } from "@/shared/models/flow-step-catalog";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

const MAX_ACTIVITY_BARS = 120;

// Every one a whole number of seconds, so a bar is a sum of the seconds counted.
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

/** Everything the page draws besides the rows. */
export interface ExecutionOverview {
  stepCount: number;
  countsByFlowStepId: Map<number, StepCounts>;
  activity: Activity;

  /** Epoch milliseconds. Undefined until a row has arrived. */
  startedAt: number | undefined;
  endedAt: number | undefined;

  foundCount: number;
  handledCount: number;
  fatalSequence: number | undefined;

  /** The longest step, which every row's bar is drawn against. */
  maxDurationMilliseconds: number;

  /** The outermost loop: the one a long execution goes round. */
  loop: LoopProgress | undefined;
}

/**
 * The overview, kept up to date a batch of rows at a time. Worked out again from every row on every
 * batch, it cost more with each one, and hours into an execution the page spent more than the
 * engine did.
 */
export class ExecutionTally {
  private stepCount = 0;
  private foundCount = 0;
  private failureCount = 0;
  private maxDurationMilliseconds = 1;
  private startedAt: number | undefined;
  private endedAt: number | undefined;
  private fatal: { sequence: number; second: number | undefined } | undefined;

  private readonly countsByFlowStepId = new Map<number, StepCounts>();

  // Counted a second at a time from the first row, whatever the bars come to.
  private readonly stepsBySecond: number[] = [];
  private readonly foundBySecond: number[] = [];

  // A pass is its loop row and every deeper row after it.
  private readonly openPasses: OpenPass[] = [];
  private readonly passesThatFoundByLoop = new Map<string, PassesThatFound>();
  private outermostLoop: ExecutionStepDto | undefined;

  add(executionSteps: ExecutionStepDto[]): void {
    for (const step of executionSteps) this.addOne(step);
  }

  /** The step that ended the execution fails last: the execution stopped there. */
  markFatal(executionSteps: ExecutionStepDto[], errorFlowStepId: number | null | undefined): void {
    if (!errorFlowStepId) return;

    for (let i = executionSteps.length - 1; i >= 0; i--) {
      const step = executionSteps[i];
      if (step.flowStepId !== errorFlowStepId || step.outcome !== StepOutcomeEnum.FAILURE) continue;

      this.fatal = { sequence: step.sequence, second: this.secondOf(Date.parse(step.startedOn)) };
      return;
    }
  }

  toOverview(): ExecutionOverview {
    const countsByFlowStepId = new Map<number, StepCounts>();
    for (const [flowStepId, counts] of this.countsByFlowStepId)
      countsByFlowStepId.set(flowStepId, { ...counts });

    return {
      stepCount: this.stepCount,
      countsByFlowStepId,
      activity: this.toActivity(),
      startedAt: this.startedAt,
      endedAt: this.endedAt,
      foundCount: this.foundCount,
      handledCount: this.failureCount - (this.fatal ? 1 : 0),
      fatalSequence: this.fatal?.sequence,
      maxDurationMilliseconds: this.maxDurationMilliseconds,
      loop: this.toLoopProgress(),
    };
  }

  private addOne(step: ExecutionStepDto): void {
    while (this.openPasses.length > 0 && this.openPasses[this.openPasses.length - 1].step.depth >= step.depth)
      this.closePass(this.openPasses.pop()!);

    const at = Date.parse(step.startedOn);
    if (this.stepCount === 0) this.startedAt = at;
    this.endedAt = at + step.durationMilliseconds;
    this.stepCount++;
    this.maxDurationMilliseconds = Math.max(this.maxDurationMilliseconds, step.durationMilliseconds);

    const isFound = isCheckStep(step) && step.outcome === StepOutcomeEnum.SUCCESS;
    if (isFound) this.foundCount++;
    if (step.outcome === StepOutcomeEnum.FAILURE) this.failureCount++;

    if (step.flowStepId && !isServedHit(step)) {
      const counts = this.countsByFlowStepId.get(step.flowStepId) ?? { executed: 0, found: 0, failed: 0 };
      counts.executed++;
      if (isFound) counts.found++;
      if (step.outcome === StepOutcomeEnum.FAILURE) counts.failed++;
      this.countsByFlowStepId.set(step.flowStepId, counts);
    }

    const second = this.secondOf(at);
    if (second !== undefined) {
      while (this.stepsBySecond.length <= second) {
        this.stepsBySecond.push(0);
        this.foundBySecond.push(0);
      }

      this.stepsBySecond[second]++;
      if (isFound) this.foundBySecond[second]++;
    }

    if (step.flowStepType !== FlowStepTypeEnum.LOOP) return;

    this.openPasses.push({ step, foundAtStart: this.foundCount });
    if (!this.outermostLoop || step.depth <= this.outermostLoop.depth) this.outermostLoop = step;
  }

  private closePass(pass: OpenPass): void {
    if (this.foundCount === pass.foundAtStart) return;

    const key = loopKeyOf(pass.step);
    const passes = this.passesThatFoundByLoop.get(key) ?? { count: 0, lastPass: 0 };
    passes.count++;
    passes.lastPass = Math.max(passes.lastPass, (pass.step.loopPass ?? 0) + 1);
    this.passesThatFoundByLoop.set(key, passes);
  }

  private secondOf(at: number): number | undefined {
    if (this.startedAt === undefined || Number.isNaN(this.startedAt) || Number.isNaN(at)) return undefined;

    return Math.max(0, Math.floor((at - this.startedAt) / 1000));
  }

  private toActivity(): Activity {
    const startedAt = this.startedAt;
    const endedAt = this.endedAt;
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

    const secondsPerBar = barMilliseconds / 1000;
    const barOf = (second: number) => bars[Math.min(bars.length - 1, Math.floor(second / secondsPerBar))];

    for (let second = 0; second < this.stepsBySecond.length; second++) {
      if (this.stepsBySecond[second] === 0) continue;

      const bar = barOf(second);
      bar.steps += this.stepsBySecond[second];
      bar.found += this.foundBySecond[second];
    }

    if (this.fatal?.second !== undefined) barOf(this.fatal.second).hasFatal = true;

    return { barMilliseconds, bars };
  }

  private toLoopProgress(): LoopProgress | undefined {
    const loop = this.outermostLoop;
    if (!loop) return undefined;

    const key = loopKeyOf(loop);
    const closed = this.passesThatFoundByLoop.get(key);
    let passesThatFound = closed?.count ?? 0;
    let lastPassThatFound = closed?.lastPass;

    // Still going, so counted as they stand.
    for (const pass of this.openPasses) {
      if (loopKeyOf(pass.step) !== key || this.foundCount === pass.foundAtStart) continue;

      passesThatFound++;
      lastPassThatFound = Math.max(lastPassThatFound ?? 0, (pass.step.loopPass ?? 0) + 1);
    }

    return {
      name: loop.name,
      pass: (loop.loopPass ?? 0) + 1,
      passesThatFound,
      lastPassThatFound,
    };
  }
}

/** A step that routes on its outcome: it either found what it looked for or went to its Failure. */
export function isCheckStep(step: ExecutionStepDto): boolean {
  return hasBranches(step.flowStepType) && !isServedHit(step);
}

/** A hit handed out from an earlier search's screenshot. Nothing executed, so it carries no time. */
export function isServedHit(step: ExecutionStepDto): boolean {
  return step.matchIndex !== null && step.matchIndex !== undefined && step.matchIndex > 0;
}

/** Two passes are of one loop when they sit at one depth and come from one step. */
export function isSameLoop(first: ExecutionStepDto, other: ExecutionStepDto): boolean {
  if (other.flowStepType !== FlowStepTypeEnum.LOOP || other.depth !== first.depth) return false;

  // Steps deleted since keep their rows with no step to point at.
  if (first.flowStepId) return other.flowStepId === first.flowStepId;
  return other.name === first.name;
}

// isSameLoop as a key, for a loop met again thousands of passes later.
function loopKeyOf(loop: ExecutionStepDto): string {
  return loop.flowStepId ? `${loop.depth}#${loop.flowStepId}` : `${loop.depth}@${loop.name}`;
}

interface OpenPass {
  step: ExecutionStepDto;

  /** Found before any step inside it. */
  foundAtStart: number;
}

interface PassesThatFound {
  count: number;

  /** From one. */
  lastPass: number;
}
