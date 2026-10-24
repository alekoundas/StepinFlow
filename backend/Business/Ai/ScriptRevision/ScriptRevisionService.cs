using System.Globalization;
using System.Text;
using Business.Ai.AiDocuments;
using Business.Ai.Helpers;
using Business.Ai.Providers;
using Business.Ai.ScriptRevision.Models;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Core.Models.Database;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Ai.ScriptRevision
{
    /// <summary>
    /// A script and an instruction in, a script that reads cleanly out - built once, for the
    /// recording's draft now and phase 9's fix loop later.
    ///
    /// The answer is JSON, so any model that can answer in it will do, with no tool calling. It is
    /// read back with the scanner as a file is, and checked against what the model may not change,
    /// in code rather than in the prompt: the flow line and id, templates it cannot have seen, and
    /// inputs renamed only through the map it answers with. What it got wrong goes back to it,
    /// ATTEMPT_COUNT attempts at most.
    ///
    /// What the code owns is never taken from the answer: the input values, which are not even
    /// sent, the description, and what each template says about its picture are put back from the
    /// script the model was given.
    /// </summary>
    public sealed class ScriptRevisionService : IScriptRevisionService
    {
        public const int ATTEMPT_COUNT = 3;

        // The format as people read it, one of the AI documents the assistant searches too.
        private const string FLOW_FORMAT_DOCUMENT = "flow-format.md";
        private static readonly Lazy<string> SYSTEM_PROMPT = new Lazy<string>(ReadSystemPrompt);

        private readonly IAiClientFactory _clientFactory;
        private readonly IScanner _scanner;
        private readonly IPrinter _printer;
        private readonly ILogger<ScriptRevisionService> _logger;

        public ScriptRevisionService(IAiClientFactory clientFactory, IScanner scanner, IPrinter printer, ILogger<ScriptRevisionService> logger)
        {
            _clientFactory = clientFactory;
            _scanner = scanner;
            _printer = printer;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ScriptRevisionResult> ReviseAsync(
            string script,
            string instruction,
            Action<int, IReadOnlyList<Diagnostic>>? onAttempt,
            CancellationToken ct = default)
        {
            ScriptRevisionResult result = new ScriptRevisionResult();

            FlowScriptSchema original = _scanner.Read(script);
            if (!original.IsValid)
            {
                result.ErrorMessage = "The script to revise does not read cleanly itself.";
                return result;
            }

            // Built per call from the settings, so it is ours to dispose.
            using IChatClient? chatClient = await _clientFactory.CreateAsync(ct);
            if (chatClient == null)
            {
                result.ErrorMessage = "AI is not set up yet. Choose a provider and a model in Settings first.";
                return result;
            }

            result.Request = RequestOf(script, instruction);

            List<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.System, SYSTEM_PROMPT.Value),
                new ChatMessage(ChatRole.User, result.Request),
            ];

            for (int attempt = 1; attempt <= ATTEMPT_COUNT; attempt++)
            {
                onAttempt?.Invoke(attempt, result.Refused);
                result.Attempts = attempt;

                ChatResponse<ScriptAnswer> response;
                try
                {
                    response = await chatClient.GetResponseAsync<ScriptAnswer>(messages, cancellationToken: ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "The model could not revise the script.");
                    result.ErrorMessage = $"The model could not be reached. {ex.Message}";
                    return result;
                }

                // Kept, so the next attempt sees what it answered beside why it was refused.
                messages.AddRange(response.Messages);

                if (response.TryGetResult(out ScriptAnswer? answer))
                {
                    Dictionary<string, string> renames = RenamesOf(answer);
                    FlowScriptSchema revised = _scanner.Read(answer.Script ?? string.Empty);

                    result.Refused = Check(original, revised, renames);
                    if (result.Refused.Count == 0)
                    {
                        PutBack(original, revised, renames);

                        result.Script = _printer.Write(revised);
                        result.Notes = NotesOf(answer);
                        result.InputRenames = renames;
                        return result;
                    }
                }
                else
                {
                    result.Refused = [Refusal(DiagnosticCodeEnum.ANSWER_UNREADABLE, "The answer was not the JSON asked for: { script, notes, inputRenames }.")];
                }

                string refusal = RefusalOf(result.Refused);
                messages.Add(new ChatMessage(ChatRole.User, refusal));
            }

            return result;
        }


        // ================================================================
        // Private methods - what is sent
        // ================================================================

        private static string ReadSystemPrompt()
        {
            string flowFormat = AiDocumentsReader.ReadDocument(FLOW_FORMAT_DOCUMENT);
            return $"{AiPromptHelper.ReviseScript}\n\n{flowFormat}";
        }

        // The model needs an input's name, never its value: an email, an account number.
        private string RequestOf(string script, string instruction)
        {
            FlowScriptSchema sent = _scanner.Read(script);
            foreach (FlowCsvColumn input in sent.Inputs)
                input.DefaultValue = string.Empty;

            string printed = _printer.Write(sent);
            return $"{instruction}\n\nThe script:\n\n{printed}";
        }

        // Line by line, so the model can find each one in what it answered.
        private static string RefusalOf(IReadOnlyList<Diagnostic> refused)
        {
            StringBuilder text = new StringBuilder("That answer was refused, and nothing was saved. Answer again with the whole script, with these fixed:");
            foreach (Diagnostic diagnostic in refused)
                text.Append(CultureInfo.InvariantCulture, $"\n- line {diagnostic.Line}, column {diagnostic.Column}, {diagnostic.Code}: {diagnostic.Message}");

            return text.ToString();
        }


        // ================================================================
        // Private methods - what comes back
        // ================================================================

        private static List<Diagnostic> Check(FlowScriptSchema original, FlowScriptSchema revised, Dictionary<string, string> renames)
        {
            List<Diagnostic> refused = revised.Diagnostics.Where(x => x.Severity == DiagnosticSeverityEnum.ERROR).ToList();
            if (refused.Count > 0)
                return refused;

            if (revised.Flow.Name != original.Flow.Name || revised.Flow.PublicId != original.Flow.PublicId)
                refused.Add(Refusal(DiagnosticCodeEnum.FLOW_CHANGED, $"Keep the Flow: and Id: lines as they were: {original.Flow.Name}, {original.Flow.PublicId}."));

            HashSet<string> cut = TemplateNamesOf(original);
            string cutNames = string.Join(", ", cut);
            foreach (string name in TemplateNamesOf(revised).Where(x => !cut.Contains(x)))
                refused.Add(Refusal(DiagnosticCodeEnum.TEMPLATE_NOT_CUT, $"<[ {name} ]> is not a template the script named. Name only these: {cutNames}."));

            refused.AddRange(CheckInputs(original, revised, renames));
            return refused;
        }

        private static List<Diagnostic> CheckInputs(FlowScriptSchema original, FlowScriptSchema revised, Dictionary<string, string> renames)
        {
            List<Diagnostic> refused = [];

            HashSet<string> had = original.Inputs.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string from in renames.Keys.Where(x => !had.Contains(x)))
                refused.Add(Refusal(DiagnosticCodeEnum.INPUT_NOT_RENAMED, $"There is no input {from} to rename."));

            HashSet<string> expected = original.Inputs.Select(x => NewName(x.Name, renames)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> answered = revised.Inputs.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string name in expected.Where(x => !answered.Contains(x)))
                refused.Add(Refusal(DiagnosticCodeEnum.INPUT_NOT_RENAMED, $"The input {name} is missing. Keep every input, and rename one only through inputRenames."));

            foreach (string name in answered.Where(x => !expected.Contains(x)))
                refused.Add(Refusal(DiagnosticCodeEnum.INPUT_NOT_RENAMED, $"The input {name} is not one the script had. Never add an input, and rename one only through inputRenames."));

            return refused;
        }

        // What the code owns, from the script the model was given.
        private static void PutBack(FlowScriptSchema original, FlowScriptSchema revised, Dictionary<string, string> renames)
        {
            revised.Flow.Description = original.Flow.Description;

            foreach (FlowCsvColumn input in original.Inputs)
            {
                string name = NewName(input.Name, renames);
                FlowCsvColumn answered = revised.Inputs.First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                answered.DefaultValue = input.DefaultValue;
                answered.IsSecret = input.IsSecret;
            }

            Dictionary<string, FlowTemplate> pictures = original.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate)
                .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (FlowTemplate template in revised.Steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate).Distinct())
            {
                FlowTemplate picture = pictures[template.Name];

                template.ClickOffsetX = picture.ClickOffsetX;
                template.ClickOffsetY = picture.ClickOffsetY;
                template.AuthoredFlowAreaWidth = picture.AuthoredFlowAreaWidth;
                template.AuthoredFlowAreaHeight = picture.AuthoredFlowAreaHeight;
                template.AuthoredDpi = picture.AuthoredDpi;
            }
        }

        private static Dictionary<string, string> RenamesOf(ScriptAnswer answer)
        {
            Dictionary<string, string> renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (InputRename rename in answer.InputRenames ?? [])
            {
                string from = rename.From?.Trim() ?? string.Empty;
                string to = rename.To?.Trim() ?? string.Empty;

                if (from.Length > 0 && to.Length > 0 && !string.Equals(from, to, StringComparison.Ordinal))
                    renames[from] = to;
            }

            return renames;
        }

        private static List<string> NotesOf(ScriptAnswer answer)
        {
            IEnumerable<string?> notes = answer.Notes ?? [];

            return notes
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => x.Length > 0)
                .ToList();
        }

        private static string NewName(string name, Dictionary<string, string> renames)
        {
            if (renames.TryGetValue(name, out string? renamed))
                return renamed;

            return name;
        }

        private static HashSet<string> TemplateNamesOf(FlowScriptSchema schema)
        {
            return schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // About the answer as a whole, so at its first line.
        private static Diagnostic Refusal(DiagnosticCodeEnum code, string message)
        {
            return Diagnostic.Error(code, 1, 1, message);
        }


        // ================================================================
        // Private types
        // ================================================================

        // The answer as the model is asked for it. Read from JSON, so nothing in it is trusted to be there.
        private sealed record ScriptAnswer(string? Script, List<string?>? Notes, List<InputRename>? InputRenames);

        private sealed record InputRename(string? From, string? To);
    }
}
