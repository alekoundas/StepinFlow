using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService.Rules
{
    /// <summary>Adding an issue, shared so every rule reports one the same way.</summary>
    public static class ValidationResultExtensions
    {
        public static void Add(
            this FlowValidationResultDto result,
            FlowStep step,
            ValidationSeverityEnum severity,
            FlowValidationCodeEnum code,
            string message)
        {
            result.Add(step.Id, step.Name, severity, code, message);
        }

        public static void Add(
            this FlowValidationResultDto result,
            int? stepId,
            string stepName,
            ValidationSeverityEnum severity,
            FlowValidationCodeEnum code,
            string message)
        {
            result.Issues.Add(new FlowValidationIssueDto
            {
                FlowStepId = stepId,
                FlowStepName = stepName,
                Severity = severity,
                Code = code,
                Message = message,
            });
        }
    }
}
