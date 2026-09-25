using System.Drawing;

using Business.AreaPoint;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Tests.AreaPoint
{
    public sealed class AreaPointResolverTests
    {
        private const string Secondary = @"\\.\DISPLAY2";

        // A 4K primary at 125%, and a 1080p monitor at 100% to its right.
        private readonly FakeScreenService _screen = new FakeScreenService()
            .WithMonitor(@"\\.\DISPLAY1", new Rectangle(0, 0, 3840, 2160), 120, isPrimary: true)
            .WithMonitor(Secondary, new Rectangle(3840, 0, 1920, 1080), 96);

        private readonly FakeWindowService _windows = new FakeWindowService();

        private AreaPointResolver Resolver()
        {
            return new AreaPointResolver(null!, _windows, _screen);
        }

        private static FlowArea Primary(ScalesWithEnum? scalesWith = null)
        {
            return new FlowArea { Id = 1, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR, ScalesWith = scalesWith };
        }

        private static FlowArea Inside(FlowArea parent, Rectangle pixels, int authoredDpi, ScalesWithEnum? scalesWith = null)
        {
            return new FlowArea
            {
                Id = 2,
                Name = "Region",
                Type = FlowAreaTypeEnum.CUSTOM,
                ParentFlowAreaId = parent.Id,
                ParentFlowArea = parent,
                SizingMode = AreaSizingModeEnum.ABSOLUTE_PX,
                LocationX = pixels.X,
                LocationY = pixels.Y,
                Width = pixels.Width,
                Height = pixels.Height,
                AuthoredDpi = authoredDpi,
                ScalesWith = scalesWith,
            };
        }

        private static FlowArea OnScreen(Rectangle pixels)
        {
            return new FlowArea { Name = "Drawn", Type = FlowAreaTypeEnum.CUSTOM, LocationX = pixels.X, LocationY = pixels.Y, Width = pixels.Width, Height = pixels.Height };
        }

        // ================================================================
        // Monitors
        // ================================================================

        [Fact]
        public void An_empty_device_name_is_the_primary_monitor()
        {
            AreaResolution area = Resolver().ResolveArea(Primary());

            area.IsResolved.ShouldBeTrue();
            area.Bounds.ShouldBe(new Rectangle(0, 0, 3840, 2160));
            area.Dpi.ShouldBe(120);
        }

        [Fact]
        public void A_named_monitor_is_found_whatever_the_case()
        {
            AreaResolution area = Resolver().ResolveArea(new FlowArea { Type = FlowAreaTypeEnum.MONITOR, MonitorDeviceName = @"\\.\display2" });

            area.Bounds.ShouldBe(new Rectangle(3840, 0, 1920, 1080));
            area.Dpi.ShouldBe(96);
        }

        [Fact]
        public void A_monitor_that_is_not_connected_fails_naming_it()
        {
            AreaResolution area = Resolver().ResolveArea(new FlowArea { Type = FlowAreaTypeEnum.MONITOR, MonitorDeviceName = @"\\.\DISPLAY9" });

            area.IsResolved.ShouldBeFalse();
            area.Error.ShouldBe(@"Monitor ""\\.\DISPLAY9"" is not connected.");
        }

        // ================================================================
        // Which DPI an area has
        // ================================================================

        [Theory]
        [InlineData(3000, 120)]
        [InlineData(3500, 96)]
        public void An_area_across_two_monitors_takes_the_DPI_of_the_one_holding_most_of_it(int x, int dpi)
        {
            Resolver().ResolveArea(OnScreen(new Rectangle(x, 0, 1000, 100))).Dpi.ShouldBe(dpi);
        }

        [Fact]
        public void An_area_on_no_monitor_takes_the_primary_DPI()
        {
            Resolver().ResolveArea(OnScreen(new Rectangle(-5000, -5000, 100, 100))).Dpi.ShouldBe(120);
        }

        [Fact]
        public void With_no_monitors_at_all_the_DPI_is_96()
        {
            _screen.Monitors.Clear();

            Resolver().ResolveArea(OnScreen(new Rectangle(0, 0, 100, 100))).Dpi.ShouldBe(96);
        }

        // ================================================================
        // What an area scales with
        // ================================================================

        [Fact]
        public void An_area_with_nothing_set_and_no_parent_scales_with_DPI()
        {
            Resolver().ResolveArea(Primary()).ScalesWith.ShouldBe(ScalesWithEnum.DPI);
        }

        [Fact]
        public void A_child_with_nothing_set_follows_its_parent()
        {
            FlowArea child = Inside(Primary(ScalesWithEnum.AREA), new Rectangle(0, 0, 10, 10), 0);

            Resolver().ResolveArea(child).ScalesWith.ShouldBe(ScalesWithEnum.AREA);
        }

        [Fact]
        public void A_child_that_says_otherwise_keeps_its_own()
        {
            FlowArea child = Inside(Primary(ScalesWithEnum.DPI), new Rectangle(0, 0, 10, 10), 0, ScalesWithEnum.AREA);

            Resolver().ResolveArea(child).ScalesWith.ShouldBe(ScalesWithEnum.AREA);
        }

        // ================================================================
        // Child areas
        // ================================================================

        [Fact]
        public void A_child_in_pixels_grows_with_a_DPI_parent_by_the_DPI_it_was_captured_at()
        {
            FlowArea child = Inside(Primary(ScalesWithEnum.DPI), new Rectangle(10, 20, 100, 50), authoredDpi: 60);

            Resolver().ResolveArea(child).Bounds.ShouldBe(new Rectangle(20, 40, 200, 100));
        }

        [Fact]
        public void A_child_in_pixels_keeps_its_pixels_inside_a_parent_that_scales_with_its_size()
        {
            FlowArea child = Inside(Primary(ScalesWithEnum.AREA), new Rectangle(10, 20, 100, 50), authoredDpi: 60);

            Resolver().ResolveArea(child).Bounds.ShouldBe(new Rectangle(10, 20, 100, 50));
        }

        [Fact]
        public void A_child_with_no_DPI_recorded_keeps_its_pixels()
        {
            FlowArea child = Inside(Primary(ScalesWithEnum.DPI), new Rectangle(10, 20, 100, 50), authoredDpi: 0);

            Resolver().ResolveArea(child).Bounds.ShouldBe(new Rectangle(10, 20, 100, 50));
        }

        [Fact]
        public void A_child_in_percent_is_a_fraction_of_its_parent()
        {
            FlowArea child = Inside(Primary(), Rectangle.Empty, 0);
            child.SizingMode = AreaSizingModeEnum.RATIO;
            child.RatioX = 0.25f;
            child.RatioY = 0.5f;
            child.RatioWidth = 0.5f;
            child.RatioHeight = 0.25f;

            Resolver().ResolveArea(child).Bounds.ShouldBe(new Rectangle(960, 1080, 1920, 540));
        }

        [Fact]
        public void A_child_is_cropped_to_its_parent_and_fails_when_nothing_is_left()
        {
            FlowArea partly = Inside(Primary(), new Rectangle(3800, 0, 100, 100), 0);
            FlowArea outside = Inside(Primary(), new Rectangle(5000, 0, 100, 100), 0);

            Resolver().ResolveArea(partly).Bounds.ShouldBe(new Rectangle(3800, 0, 40, 100));
            Resolver().ResolveArea(outside).Error.ShouldBe(@"""Region"" falls outside the area it sits in.");
        }

        // ================================================================
        // Windows
        // ================================================================

        [Fact]
        public void An_application_area_is_its_window()
        {
            _windows.Window = new WindowHandle(42);
            _windows.Bounds = new Rectangle(100, 100, 800, 600);

            AreaResolution area = Resolver().ResolveArea(new FlowArea { Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome" });

            area.Bounds.ShouldBe(new Rectangle(100, 100, 800, 600));
            _windows.Queries.Single().ProcessName.ShouldBe("chrome");
        }

        [Fact]
        public void An_application_with_no_window_fails_naming_the_area()
        {
            Resolver().ResolveArea(new FlowArea { Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION }).Error.ShouldBe(@"No window matches ""Browser"".");
        }

        [Fact]
        public void A_minimised_window_has_no_area()
        {
            _windows.Window = new WindowHandle(42);
            _windows.Bounds = Rectangle.Empty;

            Resolver().ResolveArea(new FlowArea { Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION }).IsResolved.ShouldBeFalse();
        }

        [Fact]
        public void A_browser_tab_is_not_supported_yet()
        {
            Resolver().ResolveArea(new FlowArea { Type = FlowAreaTypeEnum.BROWSER_TAB }).IsResolved.ShouldBeFalse();
        }

        // ================================================================
        // Points
        // ================================================================

        [Fact]
        public void A_point_measured_from_nothing_is_a_screen_coordinate()
        {
            PointResolution point = Resolver().ResolvePoint(new FlowPoint { LocationX = 12, LocationY = -5 });

            point.Point.ShouldBe(new Point(12, -5));
        }

        [Fact]
        public void A_point_in_pixels_grows_by_its_own_DPI_not_its_areas()
        {
            FlowArea screen = Primary(ScalesWithEnum.DPI);
            screen.AuthoredDpi = 96;

            PointResolution point = Resolver().ResolvePoint(new FlowPoint { FlowArea = screen, FlowAreaId = 1, LocationX = 100, LocationY = 40, AuthoredDpi = 60 });

            point.Point.ShouldBe(new Point(200, 80));
        }

        [Fact]
        public void A_point_in_percent_is_a_fraction_of_its_area()
        {
            PointResolution point = Resolver().ResolvePoint(new FlowPoint { FlowArea = Primary(), FlowAreaId = 1, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.5f, RatioY = 0.25f });

            point.Point.ShouldBe(new Point(1920, 540));
        }

        [Fact]
        public void A_point_outside_its_area_is_brought_to_its_edge()
        {
            PointResolution point = Resolver().ResolvePoint(new FlowPoint { FlowArea = Primary(), FlowAreaId = 1, LocationX = 9000, LocationY = -10 });

            point.Point.ShouldBe(new Point(3839, 0));
        }

        [Fact]
        public void A_point_in_an_area_that_cannot_be_found_fails_with_the_areas_reason()
        {
            PointResolution point = Resolver().ResolvePoint(new FlowPoint { FlowArea = new FlowArea { Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION }, FlowAreaId = 3 });

            point.Error.ShouldBe(@"No window matches ""Browser"".");
        }
    }
}
