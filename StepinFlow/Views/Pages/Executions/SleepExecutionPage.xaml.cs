﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages.Executions;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.Executions
{
    public partial class SleepExecutionPage : Page, IExecutionPage
    {
        public IExecutionViewModel ViewModel { get; set; }
        public SleepExecutionPage()
        {
            ViewModel = new SleepExecutionViewModel();
            InitializeComponent();
            DataContext = this;
        }
    }
}
