namespace Core.Models.Dtos
{
    public class AiModelDto
    {
        public string Name { get; set; } = string.Empty;
        public int ContextLength { get; set; }
        
        public IReadOnlyList<string> Capabilities { get; set; } = [];

        /// <summary>Why StepinFlow cannot use it, empty when it can. Every AI feature wants tools and vision.</summary>
        public string UnusableReason { get; set; } = string.Empty;

        public bool Supports(string capability)
        {
            return Capabilities.Any(x => string.Equals(x, capability, StringComparison.OrdinalIgnoreCase));
        }
    }
}
