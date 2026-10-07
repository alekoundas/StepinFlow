using Business.Tests.Fakes;
using Business.Validation;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.Validation
{
    /// <summary>
    /// Saved flows validated from the database, one at a time and in a batch. The flow list badges
    /// from the batch and Start is refused from one at a time, so the two have to agree.
    /// </summary>
    public sealed class FlowValidationLoadTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        private int Seed(string name, params FlowStep[] steps)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name };
            db.Flows.Add(flow);
            db.SaveChanges();

            foreach (FlowStep step in steps)
            {
                step.RootId = flow.Id;
                step.FlowId = flow.Id;
                db.FlowSteps.Add(step);
            }

            db.FlowAreas.Add(new FlowArea { FlowId = flow.Id, Name = name + " area", Type = FlowAreaTypeEnum.MONITOR });
            db.SaveChanges();

            return flow.Id;
        }

        private static List<FlowValidationCodeEnum> Codes(FlowValidationResultDto result)
        {
            return result.Issues.Select(x => x.Code).Order().ToList();
        }

        [Fact]
        public async Task A_batch_gives_each_flow_the_answer_it_gets_alone()
        {
            int broken = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click" });
            int fine = Seed("Fine", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });
            int empty = Seed("Empty");
            FlowValidationService validation = new FlowValidationService();

            using AppDbContext db = _database.CreateDbContext();
            IReadOnlyDictionary<int, FlowValidationResultDto> batch = await validation.ValidateAsync(db, [broken, fine, empty], Ct);

            foreach (int flowId in new[] { broken, fine, empty })
                Codes(batch[flowId]).ShouldBe(Codes(await validation.ValidateAsync(db, flowId, Ct)), $"flow {flowId}");

            batch[broken].HasErrors.ShouldBeTrue();
            batch[fine].HasErrors.ShouldBeFalse();
            Codes(batch[empty]).ShouldBe([FlowValidationCodeEnum.FLOW_HAS_NO_STEPS]);
        }

        // Each flow's areas and points are its own: a name in one flow is not a duplicate of a name
        // in another.
        [Fact]
        public async Task A_batch_keeps_each_flows_names_to_itself()
        {
            int first = Seed("Same", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });
            int second = Seed("Same", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });

            using AppDbContext db = _database.CreateDbContext();
            IReadOnlyDictionary<int, FlowValidationResultDto> batch = await new FlowValidationService().ValidateAsync(db, [first, second], Ct);

            batch[first].Issues.ShouldNotContain(x => x.Code == FlowValidationCodeEnum.NAME_DUPLICATE);
            batch[second].Issues.ShouldNotContain(x => x.Code == FlowValidationCodeEnum.NAME_DUPLICATE);
        }

        [Fact]
        public async Task A_flow_that_does_not_exist_validates_as_one_with_no_steps()
        {
            int existing = Seed("Fine", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });
            int missing = existing + 1000;
            FlowValidationService validation = new FlowValidationService();

            using AppDbContext db = _database.CreateDbContext();
            IReadOnlyDictionary<int, FlowValidationResultDto> batch = await validation.ValidateAsync(db, [existing, missing], Ct);

            Codes(batch[missing]).ShouldBe([FlowValidationCodeEnum.FLOW_HAS_NO_STEPS]);
            Codes(await validation.ValidateAsync(db, missing, Ct)).ShouldBe([FlowValidationCodeEnum.FLOW_HAS_NO_STEPS]);
            batch[existing].HasErrors.ShouldBeFalse();
        }
    }
}
