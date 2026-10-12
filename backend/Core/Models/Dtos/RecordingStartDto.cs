namespace Core.Models.Dtos
{
    public class RecordingStartDto
    {
        /// <summary>The flow whose main area is recorded against. Null records as the wizard does, with none.</summary>
        public int? FlowId { get; set; }

        /// <summary>Electron's process, which owns every StepinFlow window. Nothing done on them is recorded.</summary>
        public int ElectronProcessId { get; set; }
    }
}
