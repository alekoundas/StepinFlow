﻿using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using Business.Helpers;

namespace ModernAiClicker.ViewModels.Pages.Executions
{
    public partial class WindowResizeExecutionViewModel : ObservableObject, IExecutionViewModel
    {
        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        public WindowResizeExecutionViewModel()
        {
            _execution = new Execution();
        }

        public void SetExecution(Execution execution)
        {
            Execution = execution;
        }
    }
}
