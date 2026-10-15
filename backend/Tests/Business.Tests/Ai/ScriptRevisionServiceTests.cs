using System.Text.Json;
using Business.Ai.ScriptRevision;
using Business.Ai.ScriptRevision.Models;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Business.Tests.FlowScript;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// A model revising a script, with the model played by the test: each answer is the script it
    /// was sent, changed as the test says. The script is the sample flow, with an input whose value
    /// is standard_user and a secret one.
    /// </summary>
    public sealed class ScriptRevisionServiceTests
    {
        private const string INSTRUCTION = "Name the steps after what they do.";

        private readonly FakeAiClientFactory _factory = new FakeAiClientFactory();
        private readonly ScriptRevisionService _service;
        private readonly string _script = new Printer().Write(SampleFlow.Build());

        public ScriptRevisionServiceTests()
        {
            _service = new ScriptRevisionService(_factory, new Scanner(), new Printer(), NullLogger<ScriptRevisionService>.Instance);
        }

        private FakeChatClient Model(Func<IReadOnlyList<ChatMessage>, string> answer)
        {
            FakeChatClient client = new FakeChatClient(answer);
            _factory.Client = client;

            return client;
        }

        private Task<ScriptRevisionResult> ReviseAsync()
        {
            return _service.ReviseAsync(_script, INSTRUCTION, null, Ct);
        }

        // The script as the model was sent it, in the first request.
        private static string Sent(IReadOnlyList<ChatMessage> messages)
        {
            string request = messages.First(x => x.Role == ChatRole.User).Text;
            return request[(request.IndexOf("The script:", StringComparison.Ordinal) + "The script:".Length)..].Trim();
        }

        private static string Answer(string script, string[]? notes = null, (string From, string To)[]? renames = null)
        {
            (string From, string To)[] given = renames ?? [];
            object answer = new
            {
                script,
                notes = notes ?? [],
                inputRenames = given.Select(x => new { from = x.From, to = x.To }),
            };

            return JsonSerializer.Serialize(answer);
        }

        // ================================================================
        // Taken
        // ================================================================

        [Fact]
        public async Task A_clean_answer_is_taken_with_its_notes()
        {
            Model(x => Answer(Sent(x).Replace("Find username field", "Find the sign-in field"), ["Check the sign-in field is the first one."]));

            ScriptRevisionResult result = await ReviseAsync();

            result.IsRevised.ShouldBeTrue();
            result.Script.ShouldNotBeNull().ShouldContain("Find the sign-in field");
            result.Notes.ShouldBe(["Check the sign-in field is the first one."]);
            result.Attempts.ShouldBe(1);
        }

        // An input's value is not the model's to see, and so not its to change.
        [Fact]
        public async Task An_input_value_is_never_sent_and_is_put_back()
        {
            FakeChatClient model = Model(x => Answer(Sent(x)));

            ScriptRevisionResult result = await ReviseAsync();

            Sent(model.Requests[0]).ShouldNotContain("standard_user");
            result.Request.ShouldNotContain("standard_user");
            result.Script.ShouldNotBeNull().ShouldContain("default <[ standard_user ]>");
        }

        [Fact]
        public async Task An_input_renamed_through_the_map_keeps_its_value()
        {
            Model(x => Answer(Sent(x).Replace("username", "user_name"), renames: [("username", "user_name")]));

            ScriptRevisionResult result = await ReviseAsync();

            result.IsRevised.ShouldBeTrue();
            result.Script.ShouldNotBeNull().ShouldContain("<[ user_name ]>");
            result.Script.ShouldNotBeNull().ShouldContain("default <[ standard_user ]>");
            result.InputRenames["username"].ShouldBe("user_name");
        }

        [Fact]
        public async Task The_description_and_what_a_template_says_about_its_picture_are_put_back()
        {
            Model(x => Answer(Sent(x).Replace("Logs in to Swag Labs", "Does something else").Replace("click 60 12", "click 0 0")));

            ScriptRevisionResult result = await ReviseAsync();

            result.Script.ShouldNotBeNull().ShouldContain("Logs in to Swag Labs");
            result.Script.ShouldNotBeNull().ShouldContain("click 60 12");
        }

        // ================================================================
        // Refused
        // ================================================================

        [Fact]
        public async Task A_refused_answer_goes_back_with_its_lines_and_the_next_one_is_taken()
        {
            List<(int Attempt, int Refused)> attempts = [];
            FakeChatClient model = Model(x =>
            {
                if (x.Count == 2)
                    return Answer(Sent(x).Replace("Find Image", "Fnid Image"));

                return Answer(Sent(x));
            });

            ScriptRevisionResult result = await _service.ReviseAsync(_script, INSTRUCTION, (attempt, refused) => attempts.Add((attempt, refused.Count)), Ct);

            result.IsRevised.ShouldBeTrue();
            result.Attempts.ShouldBe(2);
            model.Requests[1].Last().Text.ShouldContain("TOKEN_UNEXPECTED");
            attempts[0].ShouldBe((1, 0));
            attempts[1].Attempt.ShouldBe(2);
            attempts[1].Refused.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task A_template_the_script_did_not_name_is_refused_three_times_and_nothing_is_taken()
        {
            FakeChatClient model = Model(x => Answer(Sent(x).Replace("template-u9d3n.png", "template-zzzzz.png")));

            ScriptRevisionResult result = await ReviseAsync();

            result.IsRevised.ShouldBeFalse();
            result.Attempts.ShouldBe(ScriptRevisionService.ATTEMPT_COUNT);
            model.Requests.Count.ShouldBe(ScriptRevisionService.ATTEMPT_COUNT);
            result.Refused.ShouldContain(x => x.Code == DiagnosticCodeEnum.TEMPLATE_NOT_CUT);
        }

        [Fact]
        public async Task An_input_renamed_without_the_map_is_refused()
        {
            Model(x => Answer(Sent(x).Replace("username", "user_name")));

            ScriptRevisionResult result = await ReviseAsync();

            result.Refused.ShouldContain(x => x.Code == DiagnosticCodeEnum.INPUT_NOT_RENAMED);
        }

        [Fact]
        public async Task The_flow_line_and_id_cannot_change()
        {
            Model(x => Answer(Sent(x).Replace("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111", "8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b222")));

            ScriptRevisionResult result = await ReviseAsync();

            result.Refused.ShouldContain(x => x.Code == DiagnosticCodeEnum.FLOW_CHANGED);
        }

        [Fact]
        public async Task An_answer_that_is_not_the_JSON_asked_for_is_refused()
        {
            Model(x => "Sure! Here is the revised flow.");

            ScriptRevisionResult result = await ReviseAsync();

            result.IsRevised.ShouldBeFalse();
            result.Refused.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.ANSWER_UNREADABLE);
        }

        // ================================================================
        // No answer at all
        // ================================================================

        [Fact]
        public async Task A_model_that_cannot_be_reached_ends_it_and_says_why()
        {
            Model(x => throw new HttpRequestException("Connection refused."));

            ScriptRevisionResult result = await ReviseAsync();

            result.IsRevised.ShouldBeFalse();
            result.Attempts.ShouldBe(1);
            result.ErrorMessage.ShouldBe("The model could not be reached. Connection refused.");
        }

        [Fact]
        public async Task With_no_model_set_up_nothing_is_asked()
        {
            ScriptRevisionResult result = await ReviseAsync();

            result.ErrorMessage.ShouldBe("AI is not set up yet. Choose a provider and a model in Settings first.");
            result.Attempts.ShouldBe(0);
        }
    }
}
