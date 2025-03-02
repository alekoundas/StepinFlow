﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Business.DatabaseContext;
using Business.Factories;
using Business.Factories.Workers;
using Business.Interfaces;
using Business.Repository;
using Business.Repository.Entities;
using Business.Repository.Interfaces;
using Business.Services;
using DataAccess.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StepinFlow.Interfaces;
using StepinFlow.Services;
using StepinFlow.ViewModels.Pages;
using StepinFlow.ViewModels.Pages.Executions;
using StepinFlow.ViewModels.Pages.FlowParameterDetail;
using StepinFlow.ViewModels.UserControls;
using StepinFlow.ViewModels.Windows;
using StepinFlow.Views.Pages;
using StepinFlow.Views.Pages.Executions;
using StepinFlow.Views.Pages.FlowDetail;
using StepinFlow.Views.Pages.FlowParameterDetail;
using StepinFlow.Views.Pages.FlowStepDetail;
using StepinFlow.Views.UserControls;
using StepinFlow.Views.Windows;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace StepinFlow
{
    public partial class App
    {

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly string? _basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.SetBasePath(_basePath ?? ""))
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                // Page resolver service
                services.AddSingleton<INavigationViewPageProvider, PageService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Windows.
                services.AddTransient<ScreenshotSelectionWindow>();
                services.AddTransient<ScreenshotSelectionWindowVM>();

                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowVM>();

                // Services.
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                services.AddScoped<ISystemService, SystemService>();
                services.AddScoped<IWindowService, WindowService>();
                services.AddScoped<ITemplateSearchService, TemplateSearchService>();
                services.AddTransient<IDataService, DataService>();
                services.AddTransient<ICloneService, CloneService>();
                services.AddTransient<IKeyboardListenerService, KeyboardListenerService>();
                services.AddSingleton<IWindowStateService, WindowStateService>();

                // Repository
                services.AddScoped<IFlowRepository, FlowRepository>();
                services.AddScoped<IFlowStepRepository, FlowStepRepository>();
                services.AddScoped<IExecutionRepository, ExecutionRepository>();
                services.AddScoped<IAppSettingRepository, AppSettingRepository>();
                services.AddScoped<IFlowParameterRepository, FlowParameterRepository>();

                // DB context
                services.AddDbContext<InMemoryDbContext>(ServiceLifetime.Transient);

                // Factory.
                services.AddScoped<IExecutionFactory, ExecutionFactory>();
                services.AddScoped<GoToExecutionWorker>();
                services.AddScoped<WaitExecutionWorker>();
                services.AddScoped<CursorRelocateExecutionWorker>();
                services.AddScoped<WindowMoveExecutionWorker>();
                services.AddScoped<CursorClickExecutionWorker>();
                services.AddScoped<CursorScrollExecutionWorker>();
                services.AddScoped<WindowResizeExecutionWorker>();
                services.AddScoped<TemplateSearchExecutionWorker>();
                services.AddScoped<MultipleTemplateSearchExecutionWorker>();
                services.AddScoped<SubFlowStepExecutionWorker>();

                // User Controls
                services.AddTransient<TreeViewUserControl>();
                services.AddTransient<TreeViewUserControlVM>();

                services.AddTransient<FrameDetailUserControl>();
                services.AddTransient<FrameDetailUserControlVM>();



                // Pages
                services.AddTransient<DashboardPage>();
                services.AddTransient<DashboardVM>();

                services.AddTransient<DataPage>();
                services.AddTransient<DataVM>();

                services.AddTransient<SubFlowsPage>();
                services.AddTransient<SubFlowsVM>();

                services.AddTransient<FlowsPage>();
                services.AddTransient<FlowsVM>();

                services.AddTransient<ExecutionPage>();
                services.AddTransient<ExecutionVM>();

                services.AddTransient<SettingsPage>();
                services.AddTransient<SettingsVM>();

                // Flow detail.
                services.AddSingleton<FlowPage>();
                services.AddSingleton<FlowVM>();

                // Flow parameter detail.
                services.AddSingleton<TemplateSearchAreaFlowParameterPage>();
                services.AddSingleton<TemplateSearchAreaFlowParameterVM>();

                // Flow step detail.
                services.AddSingleton<TemplateSearchFlowStepPage>();
                services.AddSingleton<TemplateSearchFlowStepVM>();

                services.AddSingleton<MultipleTemplateSearchFlowStepPage>();
                services.AddSingleton<MultipleTemplateSearchFlowStepVM>();

                services.AddSingleton<WaitForTemplateFlowStepPage>();
                services.AddSingleton<WaitForTemplateFlowStepVM>();

                services.AddSingleton<CursorClickFlowStepPage>();
                services.AddSingleton<CursorClickFlowStepVM>();

                services.AddSingleton<CursorRelocateFlowStepPage>();
                services.AddSingleton<CursorRelocateFlowStepVM>();

                services.AddSingleton<CursorScrollFlowStepPage>();
                services.AddSingleton<CursorScrollFlowStepVM>();

                services.AddSingleton<WaitFlowStepPage>();
                services.AddSingleton<WaitFlowStepVM>();

                services.AddSingleton<GoToFlowStepPage>();
                services.AddSingleton<GoToFlowStepVM>();

                services.AddSingleton<WindowResizeFlowStepPage>();
                services.AddSingleton<WindowResizeFlowStepVM>();

                services.AddSingleton<WindowMoveFlowStepPage>();
                services.AddSingleton<WindowMoveFlowStepVM>();

                services.AddSingleton<LoopFlowStepPage>();
                services.AddSingleton<LoopFlowStepVM>();

                services.AddSingleton<SubFlowStepPage>();
                services.AddSingleton<SubFlowStepVM>();

                //Flow execution step detail
                services.AddSingleton<TemplateSearchExecutionPage>();
                services.AddSingleton<TemplateSearchExecutionVM>();

                services.AddSingleton<MultipleTemplateSearchExecutionPage>();
                services.AddSingleton<MultipleTemplateSearchExecutionVM>();

                services.AddSingleton<WaitForTemplateExecutionPage>();
                services.AddSingleton<WaitForTemplateExecutionVM>();

                services.AddSingleton<CursorClickExecutionPage>();
                services.AddSingleton<CursorClickExecutionVM>();

                services.AddSingleton<CursorRelocateExecutionPage>();
                services.AddSingleton<CursorRelocateExecutionVM>();

                services.AddSingleton<CursorScrollExecutionPage>();
                services.AddSingleton<CursorScrollExecutionVM>();

                services.AddSingleton<WaitExecutionPage>();
                services.AddSingleton<WaitExecutionVM>();

                services.AddSingleton<GoToExecutionPage>();
                services.AddSingleton<GoToExecutionVM>();

                services.AddSingleton<WindowResizeExecutionVM>();
                services.AddSingleton<WindowResizeExecutionPage>();

                services.AddSingleton<WindowMoveExecutionVM>();
                services.AddSingleton<WindowMoveExecutionPage>();

                services.AddSingleton<LoopExecutionVM>();
                services.AddSingleton<LoopExecutionPage>();

                services.AddSingleton<SubFlowStepExecutionVM>();
                services.AddSingleton<SubFlowStepExecutionPage>();

            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T? GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            InMemoryDbContext? inMemoryDbContext = GetService<InMemoryDbContext>();

            if (inMemoryDbContext != null)
            {
                inMemoryDbContext.RunMigrations();
                inMemoryDbContext.TrySeedInitialData();
            }

            _host.Start();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
