namespace Core.Models.Dtos
{
    /// <summary>
    /// What the model said, and what it was shown. The prompt comes back so the user can see
    /// exactly what left their machine - which matters most for the provider they pay for.
    /// </summary>
    public class AiAnswerDto
    {
        public string Answer { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;

        /// <summary>Set when the model could not be reached. The Answer is empty when this is not.</summary>
        public string Error { get; set; } = string.Empty;
    }
}
