using System.Drawing;
using System.Windows.Forms;
using Core.Models.Business;
using Platform.Windows.Accessibility;
using Platform.Windows.Native;

namespace Platform.Windows.Tests.Accessibility
{
    /// <summary>
    /// UI Automation asked about a window of the test's own: a button, a check box, a password field
    /// and a disabled button in a group. The window stays on top without taking the focus, for a
    /// moment, so whatever the tester has in front keeps its keys. A live desktop only.
    /// </summary>
    [Trait("Category", "Desktop")]
    public sealed class AccessibleElementServiceTests
    {
        private const string TITLE = "Checkout - StepinFlow test";

        public AccessibleElementServiceTests()
        {
            ScreenMetrics.EnablePerMonitorDpiAwareness();
        }

        [Fact]
        public async Task Each_control_is_said_as_a_screen_reader_would_say_it()
        {
            Dictionary<string, AccessibleElement?> answers = await AskAboutControlsAsync();

            AccessibleElement button = answers["Add to cart"].ShouldNotBeNull();
            (button.Role, button.Name, button.IsEnabled, button.IsChecked).ShouldBe(("button", "Add to cart", true, null));
            button.Inside.Select(x => (x.Role, x.Name)).ShouldBe([("group", "Shipping"), ("window", TITLE)]);

            AccessibleElement check = answers["Remember me"].ShouldNotBeNull();
            (check.Role, check.IsChecked).ShouldBe(("check box", true));

            AccessibleElement password = answers["Password"].ShouldNotBeNull();
            (password.Role, password.Name, password.IsPassword).ShouldBe(("text field", "Password", true));

            answers["Pay now"].ShouldNotBeNull().IsEnabled.ShouldBeFalse();
        }

        // On a thread of its own with a message loop, which is what answers UI Automation for the
        // window, and asked from the pool, never from that thread.
        private static async Task<Dictionary<string, AccessibleElement?>> AskAboutControlsAsync()
        {
            TaskCompletionSource<Dictionary<string, AccessibleElement?>> answered = new TaskCompletionSource<Dictionary<string, AccessibleElement?>>(TaskCreationOptions.RunContinuationsAsynchronously);

            Thread thread = new Thread(() =>
            {
                using QuietForm form = new QuietForm { Text = TITLE, Width = 500, Height = 300, StartPosition = FormStartPosition.Manual, Left = 20, Top = 20 };
                GroupBox shipping = new GroupBox { Text = "Shipping", Left = 10, Top = 10, Width = 450, Height = 200 };
                Control[] controls =
                [
                    new Button { Text = "Add to cart", Left = 20, Top = 30, Width = 150, Height = 40 },
                    new CheckBox { Text = "Remember me", Left = 20, Top = 90, Width = 150, Checked = true },
                    new TextBox { Left = 20, Top = 130, Width = 150, UseSystemPasswordChar = true, AccessibleName = "Password" },
                    new Button { Text = "Pay now", Left = 200, Top = 30, Width = 150, Height = 40, Enabled = false },
                ];
                shipping.Controls.AddRange(controls);
                form.Controls.Add(shipping);

                form.Shown += async (sender, e) =>
                {
                    try
                    {
                        AccessibleElementService service = new AccessibleElementService();
                        Dictionary<string, AccessibleElement?> answers = new Dictionary<string, AccessibleElement?>();

                        foreach (Control control in controls)
                        {
                            Point centre = control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
                            answers[control.AccessibleName ?? control.Text] = await Task.Run(() => service.ElementAt(centre));
                        }

                        answered.SetResult(answers);
                    }
                    catch (Exception ex)
                    {
                        answered.SetException(ex);
                    }
                    finally
                    {
                        form.Close();
                    }
                };

                Application.Run(form);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return await answered.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        }

        private sealed class QuietForm : Form
        {
            private const int WS_EX_TOPMOST = 0x00000008;
            private const int WS_EX_NOACTIVATE = 0x08000000;

            protected override bool ShowWithoutActivation
            {
                get { return true; }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams parameters = base.CreateParams;
                    parameters.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE;
                    return parameters;
                }
            }
        }
    }
}
