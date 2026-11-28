import type { FlowStepUseDto } from "@/shared/models/flow-step-use-dto";

/** The steps among these that use a part, by the test given. */
const stepsUsing = (
  uses: FlowStepUseDto[] | undefined,
  isUsing: (use: FlowStepUseDto) => boolean,
): FlowStepUseDto[] => (uses ?? []).filter(isUsing);

/** The names of the steps that search for a picture, leaving out the one asking. */
const stepsSearchingFor = (
  uses: FlowStepUseDto[] | undefined,
  templateName: string,
  exceptFlowStepId?: number,
): string[] =>
  (uses ?? [])
    .filter((x) => x.flowStepId !== exceptFlowStepId && x.templateNames.includes(templateName))
    .map((x) => x.name);

/** Whether a step fills in this input. Names match the way the execution matches them. */
const typesInput = (use: FlowStepUseDto, name: string): boolean =>
  use.variableNames.some((x) => x.toLowerCase() === name.toLowerCase());

export const FlowParts = {
  stepsUsing,
  stepsSearchingFor,
  typesInput,
};
