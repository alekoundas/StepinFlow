using System.Windows;
using Business.Services.Interfaces;
using Model.Structs;
using StepinFlow.ViewModels.Windows;

namespace StepinFlow.Views.Windows
{
    public partial class ScreenshotSelectionWindow : Window
    {
        private readonly ISystemSettingsService _systemSettingsService;
        public ScreenshotSelectionWindowVM ViewModel { get; }

        public ScreenshotSelectionWindow(ScreenshotSelectionWindowVM viewModel, ISystemSettingsService systemSettingsService)
        {
            _systemSettingsService = systemSettingsService;


            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

            SetWindowLocation();

            viewModel.CloseWindow += () => CloseWindow();
        }
        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowLocation();
        }



        private void SetWindowLocation()
        {
            WindowSize windowState = _systemSettingsService.GetSelectorWindowState();

            this.Left = windowState.Left;
            this.Top = windowState.Top;
            this.Width = windowState.Width;
            this.Height = windowState.Height;
            this.WindowState = windowState.IsMaximized ? WindowState.Maximized : WindowState.Normal;

            if (
                this.Left < SystemParameters.VirtualScreenLeft ||
                this.Top < SystemParameters.VirtualScreenTop ||
                this.Left + this.Width > SystemParameters.VirtualScreenWidth ||
                this.Top + this.Height > SystemParameters.VirtualScreenHeight)
            {
                this.Left = 0;
                this.Top = 0;
                this.Width = 800;
                this.Height = 450;
            }
        }

        private void SaveWindowLocation()
        {
            WindowSize windowState = new WindowSize
            {
                Left = this.Left,
                Top = this.Top,
                Width = this.Width,
                Height = this.Height,
                IsMaximized = this.WindowState == WindowState.Maximized
            };
            _systemSettingsService.SaveSelectorWindowState(windowState);
        }


        private void CloseWindow()
        {
            SaveWindowLocation();
            this.Close();
        }
    }
}