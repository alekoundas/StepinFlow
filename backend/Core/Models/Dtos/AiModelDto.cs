namespace Core.Models.Dtos
{
    public class AiModelDto
    {
        public string Name { get; set; } = string.Empty;
        public int ContextLength { get; set; }
        
        ☻public IReadOnlyList<string> Capabilities { get; set; } = [];

        public bool Supports(string capability)
        {
            return Capabilities.Any(x => string.Equals(x, capability, StringComparison.OrdinalIgnoreCase));
        }
    }
}
