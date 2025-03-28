﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class CursorScrollFlowStepPage : Page, IFlowStepDetailPage, IDetailPage
    {
        public IFlowDetailVM? FlowViewModel { get; set; }
        public IFlowStepDetailVM? FlowStepViewModel { get; set; }
        public IFlowParameterDetailVM? FlowParameterViewModel { get; set; }
        public IExecutionViewModel? FlowExecutionViewModel { get; set; }


        public IFlowStepDetailVM ViewModel { get; set; }
        public CursorScrollFlowStepPage(CursorScrollFlowStepVM viewModel)
        {
            ViewModel = viewModel;
            FlowStepViewModel = ViewModel;
            InitializeComponent();
            DataContext = this;
        }
    }
}
