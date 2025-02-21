﻿using Model.Models;
using StepinFlow.ViewModels.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace StepinFlow.Views.UserControls
{
    public partial class TreeViewUserControl : UserControl
    {
        public static readonly DependencyProperty IsLockedProperty = DependencyProperty.Register(
            nameof(IsLocked),
            typeof(bool),
            typeof(TreeViewUserControl),
            new PropertyMetadata(false, OnIsLockedChanged)
        );

        public bool IsLocked
        {
            get => (bool)GetValue(IsLockedProperty);
            set => SetValue(IsLockedProperty, value);
        }


        public event EventHandler<int>? OnSelectedFlowStepIdChange;
        public event EventHandler<int>? OnSelectedFlowIdChange;
        public event EventHandler<int>? OnFlowStepClone;
        public event EventHandler<FlowStep>? OnAddFlowStepClick;
        public event EventHandler<FlowParameter>? OnAddFlowParameterClick;
        public TreeViewUserControlViewModel? ViewModel { get; set; }

        public TreeViewUserControl()
        {
            // Resolve the ViewModel from the DI container.
            ViewModel = App.GetService<TreeViewUserControlViewModel>();

            if (ViewModel == null)
            {
                throw new InvalidOperationException("Failed to resolve TreeViewUserControlViewModel from DI container.");
            }

            DataContext = this;
            InitializeComponent();
            ViewModel.OnSelectedFlowStepIdChangedEvent += OnSelectedFlowStepIdChangedEvent;
            ViewModel.OnSelectedFlowIdChangedEvent += OnSelectedFlowIdChangedEvent;
            ViewModel.OnFlowStepCloneEvent += OnFlowStepCloneEvent;
            ViewModel.OnAddFlowStepClickEvent += OnAddFlowStepClickEvent;

        }

        public async Task LoadFlows(int? id = 0) => await ViewModel!.LoadFlows(id);
        public void ClearCopy() => ViewModel!.ClearCopy();
        public async Task ExpandAll() => await ViewModel!.ExpandAll();
        public async Task CollapseAll() => await ViewModel!.CollapseAll();


        public void OnSelectedFlowStepIdChangedEvent(int id)
        {
            OnSelectedFlowStepIdChange?.Invoke(this, id);
        }
        public void OnSelectedFlowIdChangedEvent(int id)
        {
            OnSelectedFlowIdChange?.Invoke(this, id);
        }

        public void OnFlowStepCloneEvent(int id)
        {
            OnFlowStepClone?.Invoke(this, id);
        }

        public void OnAddFlowStepClickEvent(FlowStep adddFlowSttep)
        {
            OnAddFlowStepClick?.Invoke(this, adddFlowSttep);
        }
        public void OnAddFlowParameterClickEvent(FlowParameter addFlowParameter)
        {
            OnAddFlowParameterClick?.Invoke(this, addFlowParameter);
        }


        private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TreeViewUserControl)d;
            var viewModel = control.DataContext as TreeViewUserControlViewModel;

            if (viewModel != null)
                viewModel.IsLocked = (bool)e.NewValue;
        }
    }
}
