using System.Drawing;

using Core.Enums;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Dtos;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.FlowSteps
{
    /// <summary>
    /// The Move and Drag forms' Test: the real cursor between saved locations, with no execution
    /// behind it. Nothing moves unless every end can be worked out.
    /// </summary>
    public sealed class TestCursorStepHandlerTests
    {
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeInputSimulatorService _input = new FakeInputSimulatorService();

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        private Task<ResultDto<bool>> TestAsync(FlowStepDto dto)
        {
            return new TestCursorStepHandler(_resolver, _input).HandleAsync(dto, Ct);
        }

        [Fact]
        public async Task A_test_drags_between_saved_locations_as_an_execution_would()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(1, 2));
            _resolver.Points[6] = PointResolution.Ok(new Point(3, 4));

            ResultDto<bool> result = await TestAsync(new FlowStepDto { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = 5, FlowPointEndId = 6 });

            result.IsSuccess.ShouldBeTrue();
            _input.Actions.ShouldBe(["move 1,2", "drag 1,2 to 3,4 LEFT_BUTTON"]);
        }

        // A step result exists only while the flow executes.
        [Fact]
        public async Task A_test_of_a_drag_to_a_step_result_is_refused_and_does_nothing()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(1, 2));

            ResultDto<bool> result = await TestAsync(new FlowStepDto { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = 5, FlowStepReferenceEndId = 9 });

            result.IsSuccess.ShouldBeFalse();
            _input.Actions.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_drag_whose_end_cannot_be_worked_out_never_starts()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(1, 2));

            ResultDto<bool> result = await TestAsync(new FlowStepDto { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = 5, FlowPointEndId = 6 });

            result.ErrorMessage.ShouldBe("The point no longer exists.");
            _input.Actions.ShouldBeEmpty();
        }
    }
}
