import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import type { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type {
  ImageSearchTestImageDto,
  ImageSearchTestMatchDto,
  ImageSearchTestResultDto,
} from "@/shared/models/database/image-search-test-result-dto";
import { searchImageModeOf } from "@/features/flow-step/components/forms/shared/search-modes";

const MIN_ACCURACY = 0.1;
const MAX_ACCURACY = 1;

// Closer than this, the closest wrong place looks so like the template that no accuracy holds for
// long: a little light or a theme change moves both scores past it.
const NARROW_GAP = 0.12;

/** What a test was asked: the step's templates and mode as they were when Test was pressed. */
export interface ImageSearchTestRun {
  images: FlowStepTemplateDto[];
  searchMode: SearchModeEnum;
  maxMatches: number;
}

export interface AccuracySuggestion {
  accuracy: number;
  reason: string;
  isNarrow: boolean;
}

export interface TestVerdict {
  severity: "success" | "warn";
  text: string;
}

export const hitsOf = (image: ImageSearchTestImageDto): ImageSearchTestMatchDto[] =>
  image.matches.filter((x) => x.isAccepted);

/** The best candidate under the bar. The search keeps one, the highest. */
export const nearMissOf = (image: ImageSearchTestImageDto): ImageSearchTestMatchDto | null =>
  image.matches.find((x) => !x.isAccepted) ?? null;

export const nameOf = (run: ImageSearchTestRun, result: ImageSearchTestResultDto, index: number) =>
  run.images[index]?.flowTemplate.name || result.images[index]?.name || `Template ${index + 1}`;

/**
 * The bar halfway between the lowest hit and the closest wrong place, so either can move a little
 * and the answer stays the same. With no hit, a bar just under the closest place - which is only
 * right if that place is the thing, and only the person looking can tell.
 */
export function suggestAccuracy(image: ImageSearchTestImageDto): AccuracySuggestion | null {
  const nearMiss = nearMissOf(image);
  if (nearMiss === null) return null;

  const hits = hitsOf(image);
  if (hits.length === 0) {
    const accuracy = clamp(Math.floor((nearMiss.score - 0.01) * 100) / 100);

    return {
      accuracy,
      reason: `If the dashed box is the right thing, ${accuracy.toFixed(2)} finds it.`,
      isNarrow: false,
    };
  }

  const lowest = Math.min(...hits.map((x) => x.score));
  const accuracy = clamp(Math.round(((lowest + nearMiss.score) / 2) * 100) / 100);

  return {
    accuracy,
    reason: `Halfway between its hit at ${lowest.toFixed(2)} and the closest wrong place at ${nearMiss.score.toFixed(2)}.`,
    isNarrow: lowest - nearMiss.score < NARROW_GAP,
  };
}

/** The templates an execution would act on: the first that matched, or every one for Every match. */
export function usedIndexes(result: ImageSearchTestResultDto, mode: SearchModeEnum): number[] {
  const found = result.images.flatMap((x, index) => (x.isFound ? [index] : []));

  if (mode === SearchModeEnum.WAIT_UNTIL_NOT_FOUND) return [];
  if (mode === SearchModeEnum.FIND_ALL) return found;

  return found.slice(0, 1);
}

/** What an execution would do with this screenshot, in the words it would report. */
export function verdictOf(result: ImageSearchTestResultDto, run: ImageSearchTestRun): TestVerdict {
  const first = result.images.findIndex((x) => x.isFound);
  const firstText =
    first < 0
      ? ""
      : `${nameOf(run, result, first)} found at ${result.images[first].bestScore.toFixed(2)}, needs ${run.images[first]?.accuracy.toFixed(2)}`;

  switch (run.searchMode) {
    case SearchModeEnum.WAIT_UNTIL_NOT_FOUND:
      if (first < 0)
        return { severity: "success", text: "Would succeed: no template passes its accuracy, so it is gone." };

      return { severity: "warn", text: `Would keep waiting: ${firstText}.` };

    case SearchModeEnum.FIND_ALL: {
      if (first < 0)
        return { severity: "warn", text: "Would go on to the Failure steps: no template passed its accuracy." };

      const hits = result.images.reduce((total, x) => total + hitsOf(x).length, 0);
      if (hits > run.maxMatches)
        return { severity: "success", text: `Would succeed: ${hits} hits, and the first ${run.maxMatches} are worked through.` };

      return { severity: "success", text: `Would succeed: ${hits} hit${hits === 1 ? "" : "s"}, each worked through.` };
    }

    case SearchModeEnum.WAIT_UNTIL_FOUND:
      if (first < 0)
        return { severity: "warn", text: "Would keep trying: no template passed its accuracy yet." };

      return { severity: "success", text: `Would succeed: ${firstText}.` };

    case SearchModeEnum.WAIT_UNTIL_STILL:
      if (first < 0)
        return { severity: "warn", text: "Would keep trying: no template passed its accuracy yet." };

      return { severity: "success", text: `Would succeed once it is still: ${firstText}.` };

    default:
      if (first < 0)
        return { severity: "warn", text: "Would go on to the Failure steps: no template passed its accuracy." };

      return { severity: "success", text: `Would succeed: ${firstText}.` };
  }
}

/** What one test is of the mode it was made in. */
export function modeLineOf(run: ImageSearchTestRun): string {
  const label = searchImageModeOf(run.searchMode)?.label ?? run.searchMode;

  switch (run.searchMode) {
    case SearchModeEnum.FIND_ALL:
      return `${label} · one screenshot · every template's hits, up to ${run.maxMatches} in all`;

    case SearchModeEnum.WAIT_UNTIL_FOUND:
      return `${label} · one try of what an execution repeats until a template matches`;

    case SearchModeEnum.WAIT_UNTIL_STILL:
      return `${label} · one try of what an execution repeats until a template matches in the same place twice`;

    case SearchModeEnum.WAIT_UNTIL_NOT_FOUND:
      return `${label} · one try of what an execution repeats until no template matches`;

    default:
      return `${label} · one screenshot · the first template in the list that matches is used`;
  }
}

/** Whether the form's templates are still the ones the test was made with, in the same order. */
export function isSameTemplates(run: ImageSearchTestRun, current: FlowStepTemplateDto[]): boolean {
  if (run.images.length !== current.length) return false;

  return run.images.every(
    (x, index) =>
      x.flowTemplateId === current[index].flowTemplateId &&
      x.flowTemplate.templateImage === current[index].flowTemplate.templateImage,
  );
}

function clamp(accuracy: number): number {
  return Math.min(MAX_ACCURACY, Math.max(MIN_ACCURACY, accuracy));
}
