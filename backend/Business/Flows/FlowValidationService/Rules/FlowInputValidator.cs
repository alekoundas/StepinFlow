using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Flows.FlowValidationService.Rules
{
    /// <summary>
    /// An input a step types needs a value, or that step stops the execution when it gets there.
    /// Said on the input rather than on each step, since the input's row is where it is fixed: a
    /// secret's value is typed there once and never shown again.
    /// </summary>
    public static class FlowInputValidator
    {
        public static void Validate(IReadOnlyList<FlowStep> steps, IReadOnlyList<FlowInput> inputs, FlowValidationResultDto result)
        {
            HashSet<string> typed = steps
                .SelectMany(VariableTranslator.VariableBearingText)
                .SelectMany(VariableTranslator.Names)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (FlowInput input in inputs.Where(x => typed.Contains(x.Name.Trim())))
            {
                string written = "{{" + input.Name.Trim() + "}}";

                if (input.IsSecret && input.SecretValue.Length == 0)
                {
                    result.Add(null, input.Name, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.INPUT_VALUE_MISSING,
                        $"This secret has no value yet, so the steps that type {written} have nothing to type. Set it once on the Inputs tab: it stays in this machine's database and is never shown again.");
                }
                else if (!input.IsSecret && input.DefaultValue.Length == 0)
                {
                    result.Add(null, input.Name, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.INPUT_VALUE_MISSING,
                        $"This input has no value, so the steps that type {written} have nothing to type. Give it one on the Inputs tab.");
                }
            }
        }
    }
}
