using Business.Notification;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.Notification
{
    public sealed class NotifyMessageBuilderTests
    {
        [Fact]
        public void A_long_output_is_cut_and_the_users_line_is_kept()
        {
            FlowStep command = new FlowStep { Name = "Dump", FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND };
            ExecutionStep ran = ExecutionStep.Success();
            ran.ExitCode = 0;
            ran.Value = new string('x', 5000);

            string content = NotifyMessageBuilder.Build("Nightly", "look at the server", command, ran);

            content.Length.ShouldBeLessThanOrEqualTo(DiscordNotifier.MaxContentLength);
            content.ShouldContain("(truncated)");
            content.ShouldEndWith("\n\nlook at the server");
        }

        [Fact]
        public void With_no_step_to_report_on_it_is_the_flow_name_and_the_message()
        {
            NotifyMessageBuilder.Build("Nightly", " all done ", null, null).ShouldBe("**Nightly**\nall done");
        }

        [Fact]
        public void A_step_not_executed_yet_says_there_is_no_result()
        {
            FlowStep search = new FlowStep { Name = "Find login", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE };

            NotifyMessageBuilder.Build("Nightly", string.Empty, search, null).ShouldBe("**Nightly — Find login — Search Image**\nNo result yet: it has not been executed.");
        }
    }
}
