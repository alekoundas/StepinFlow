﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages.Executions;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.Executions
{
    public partial class CursorClickExecutionPage : Page, IExecutionPage, IDetailPage
    {
        public IExecutionViewModel ViewModel { get; set; }

        public IFlowDetailVM? FlowViewModel { get; set; }
        public IFlowStepDetailVM? FlowStepViewModel { get; set; }
        public IFlowParameterDetailVM? FlowParameterViewModel { get; set; }
        public IExecutionViewModel? FlowExecutionViewModel { get; set; }

        public CursorClickExecutionPage()
        {
            ViewModel = new CursorClickExecutionVM();
            FlowExecutionViewModel = ViewModel;
            InitializeComponent();
            DataContext = this;
        }
    }
}
