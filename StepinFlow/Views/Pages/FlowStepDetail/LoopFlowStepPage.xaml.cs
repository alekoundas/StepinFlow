﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class LoopFlowStepPage : Page, IPage
    {

        public IFlowStepViewModel ViewModel { get; set; }
        public LoopFlowStepPage(LoopFlowStepViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
