namespace Core.Models.Dtos
{
    public class FlowInputDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>The value, written into the script. Never set when IsSecret.</summary>
        public string DefaultValue { get; set; } = string.Empty;

        public bool IsSecret { get; set; }

        /// <summary>Whether a secret has its value saved. The value itself never comes back.</summary>
        public bool HasSecretValue { get; set; }

        /// <summary>A secret's value to save. Empty keeps the saved one.</summary>
        public string NewSecretValue { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int FlowId { get; set; }
    }
}
