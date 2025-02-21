﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class CursorMoveFlowStepPage : Page, IFlowStepDetailPage
    {
        public IFlowStepDetailVM ViewModel { get; set; }
        public CursorMoveFlowStepPage(CursorMoveFlowStepViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            DataContext = this;
        }
    }
}
