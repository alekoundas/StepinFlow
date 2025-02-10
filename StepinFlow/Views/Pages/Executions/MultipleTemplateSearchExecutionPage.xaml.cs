﻿using Business.Interfaces;
using DataAccess.Repository.Interface;
using StepinFlow.ViewModels.Pages.Executions;
using System.Net.Cache;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace StepinFlow.Views.Pages.Executions
{
    public partial class MultipleTemplateSearchExecutionPage : Page, IExecutionPage
    {
        public MultipleTemplateSearchExecutionViewModel ViewModel { get; set; }

        public MultipleTemplateSearchExecutionPage(IBaseDatawork baseDatawork)
        {
            ViewModel = new MultipleTemplateSearchExecutionViewModel(baseDatawork);
            DataContext = ViewModel;
            InitializeComponent();
        }

        public void SetViewModel(IExecutionViewModel executionViewModel)
        {
            ViewModel.ShowResultImage -= ShowResultImage;
            ViewModel = (MultipleTemplateSearchExecutionViewModel)executionViewModel;
            ViewModel.ShowResultImage += ShowResultImage;
            DataContext = ViewModel;
        }

        public void ShowResultImage(string filePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.None;
                bitmap.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                this.UIResultImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }
    }
}
