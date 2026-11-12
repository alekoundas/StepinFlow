using Business.Ai.AiDocuments;
using Business.FlowScript;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// The flows the model is shown as examples. One that stopped reading after a format change
    /// would teach the model lines the app refuses, so every one is scanned as an import is.
    /// </summary>
    public sealed class FlowExamplesTests
    {
        private const string FOLDER = ".AiDocuments.Examples.";

        // Every example in the folder, so one added later is held to this without being listed here.
        public static TheoryData<string> Examples()
        {
            TheoryData<string> examples = new TheoryData<string>();

            foreach (string name in typeof(AiDocumentChunk).Assembly.GetManifestResourceNames().Where(x => x.Contains(FOLDER, StringComparison.Ordinal)))
                examples.Add(name[(name.IndexOf(FOLDER, StringComparison.Ordinal) + FOLDER.Length)..]);

            return examples;
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public void Every_flow_in_an_example_reads_cleanly(string document)
        {
            string text = AiDocumentsReader.ReadDocument(document);
            List<string> fences = text.Replace("\r\n", "\n").Split('\n')
                .Where(x => x.StartsWith("```" + FlowExamples.FINISHED, StringComparison.Ordinal))
                .Select(x => x.TrimEnd()[3..])
                .ToList();

            fences.ShouldContain(FlowExamples.FINISHED);

            foreach (string fence in fences)
            {
                FlowScriptSchema schema = new Scanner().Read(FlowExamples.ScriptOf(text, fence));

                List<string> said = schema.Diagnostics.Select(x => $"{fence} line {x.Line}: {x.Severity} {x.Code} {x.Message}").ToList();
                said.ShouldBeEmpty();
            }
        }

        // As an import would save it, its validation included. A Discord bot is the machine's own, so
        // no example names one, and a text search's OCR language is picked in the step's form, so
        // every import is missing those two.
        [Theory]
        [MemberData(nameof(Examples))]
        public async Task Every_finished_example_imports_with_nothing_wrong_in_it(string document)
        {
            using TestDatabase database = new TestDatabase();
            string script = FlowExamples.ScriptOf(AiDocumentsReader.ReadDocument(document), FlowExamples.FINISHED);

            Dictionary<string, byte[]> pictures = new Scanner().Read(script).Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate.Name)
                .Distinct()
                .ToDictionary(x => x, x => new byte[] { 1 });

            FlowScriptImportResultDto imported = await new FlowScriptImporter(new Scanner(), TestDataService.For(database)).ImportTextAsync(script, pictures, Ct);

            imported.IsSuccess.ShouldBeTrue();
            imported.Warnings.ShouldBeEmpty();
            imported.Validation.ShouldNotBeNull().Issues
                .Where(x => x.Code != FlowValidationCodeEnum.DISCORD_BOT_MISSING)
                .Select(x => $"{x.FlowStepName}: {x.Severity} {x.Code} {x.Message}")
                .ShouldBeEmpty();
        }

        [Fact]
        public void A_game_is_shown_the_game_and_anything_else_the_website()
        {
            FlowExamples.DocumentFor(new FlowArea { Type = FlowAreaTypeEnum.APPLICATION, ScalesWith = ScalesWithEnum.AREA }).ShouldBe(FlowExamples.GAME);
            FlowExamples.DocumentFor(new FlowArea { Type = FlowAreaTypeEnum.APPLICATION, ScalesWith = ScalesWithEnum.DPI }).ShouldBe(FlowExamples.WEBSITE);
            FlowExamples.DocumentFor(new FlowArea { Type = FlowAreaTypeEnum.BROWSER_TAB }).ShouldBe(FlowExamples.WEBSITE);
            FlowExamples.DocumentFor(null).ShouldBe(FlowExamples.WEBSITE);
        }

        // The draft before the fix is for a person reading the example, not a flow to copy.
        [Fact]
        public void A_request_carries_the_finished_flow_and_never_the_one_as_recorded()
        {
            string request = FlowExamples.ForRequest(new FlowArea { Type = FlowAreaTypeEnum.BROWSER_TAB });

            request.ShouldContain("<[ Read the price ]>");
            request.ShouldNotContain("Check 104.12");
        }

        // What a recording's words are written like: a small model copies better than it follows.
        [Fact]
        public void A_recording_s_words_are_shown_how_a_finished_flow_names_its_searches()
        {
            string words = FlowExamples.WordsOf(FlowExamples.WEBSITE);

            words.ShouldContain("- Read the price: The price changes from one execution to the next");
            words.ShouldContain("Not found: no crude oil price was shown within 10 seconds");
            words.ShouldContain("never its names");
            words.ShouldNotContain("Check 104.12");
        }

        // ================================================================
        // Picked by meaning
        // ================================================================

        private static FlowExampleIndex Index(params string[] words)
        {
            FakeEmbeddingService embedder = new FakeEmbeddingService { IsReady = true };
            embedder.Words.AddRange(words);

            return new FlowExampleIndex(embedder);
        }

        [Fact]
        public void The_example_nearest_what_the_flow_is_for_is_picked()
        {
            FlowExampleIndex index = Index("game", "website", "browser", "notification", "price");
            Flow flow = new Flow { Name = "Claim rewards", Description = "Opens every notification in the game and claims it." };

            index.DocumentFor(flow, new FlowArea { Type = FlowAreaTypeEnum.APPLICATION, ScalesWith = ScalesWithEnum.DPI }).ShouldBe(FlowExamples.GAME);
        }

        // A word or two says nothing to match by.
        [Fact]
        public void With_too_little_to_go_on_the_rule_picks()
        {
            FlowExampleIndex index = Index("game", "website", "browser", "notification", "price");
            Flow flow = new Flow { Name = "Claim rewards", Description = "game" };

            index.DocumentFor(flow, new FlowArea { Type = FlowAreaTypeEnum.BROWSER_TAB }).ShouldBe(FlowExamples.WEBSITE);
        }

        [Fact]
        public void Without_an_embedding_model_the_rule_picks()
        {
            FlowExampleIndex index = new FlowExampleIndex(new FakeEmbeddingService());
            Flow flow = new Flow { Name = "Claim rewards", Description = "Opens every notification in the game and claims it." };

            index.DocumentFor(flow, new FlowArea { Type = FlowAreaTypeEnum.BROWSER_TAB }).ShouldBe(FlowExamples.WEBSITE);
        }
    }
}
