using Business.Services;
using Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Model.Structs;
using StepinFlow.ViewModels.Windows;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StepinFlow.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private readonly ISystemSettingsService _windowStateService;
        public MainWindowVM ViewModel { get; }

        public MainWindow(
            MainWindowVM viewModel,
            INavigationViewPageProvider pageService,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            IWindowStateService windowStateService
        )
        {
            _windowStateService = windowStateService;


            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            var pageProvider = serviceProvider.GetRequiredService<INavigationViewPageProvider>();

            // NavigationControl is x:Name of our NavigationView defined in XAML.
            RootNavigation.SetPageProviderService(pageProvider);
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);
            WindowSize windowState = _windowStateService.GetMainWindowState();

            this.Left = windowState.Left;
            this.Top = windowState.Top;
            this.Width = windowState.Width;
            this.Height = windowState.Height;
            this.WindowState = windowState.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            EnsureWindowInBounds();

        }


        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        //public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();


        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);


            WindowSize windowState = new WindowSize
            {
                Left = this.Left,
                Top = this.Top,
                Width = this.Width,
                Height = this.Height,
                IsMaximized = this.WindowState == WindowState.Maximized
            };
            _windowStateService.SaveMainWindowState(windowState);




            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }


        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            //RootNavigation.SetPageProviderService(serviceProvider);
            throw new NotImplementedException();
        }

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider)
        {
            RootNavigation.SetPageProviderService(navigationViewPageProvider);
        }


        private void EnsureWindowInBounds()
        {
            if (this.Left < SystemParameters.VirtualScreenLeft ||
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
    }
}
