﻿using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class TemplateSearchFlowStepPage : Page, IFlowStepDetailPage
    {
        public IFlowStepDetailVM ViewModel { get; set; }

        public TemplateSearchFlowStepPage(TemplateSearchFlowStepViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
