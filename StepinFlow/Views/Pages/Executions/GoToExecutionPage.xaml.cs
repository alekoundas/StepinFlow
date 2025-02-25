﻿using Business.Interfaces;
using DataAccess.Repository.Interface;
using StepinFlow.ViewModels.Pages.Executions;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.Executions
{
    public partial class GoToExecutionPage : Page, IExecutionPage
    {
        public IExecutionViewModel ViewModel { get; set; }
        public GoToExecutionPage(IBaseDatawork baseDatawork)
        {
            ViewModel = new GoToExecutionVM(baseDatawork);
            InitializeComponent();
            DataContext = this;
        }
    }
}
