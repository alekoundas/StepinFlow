namespace Business.FlowScript.Text.Header
{
    internal sealed class DiscordBotWriter : BaseWriter
    {
        private readonly string _name;

        public DiscordBotWriter(string name)
        {
            _name = name;
        }

        protected override void Compose()
        {
            // <[ Alerts ]>
            WriteQuote(_name);
        }
    }
}
