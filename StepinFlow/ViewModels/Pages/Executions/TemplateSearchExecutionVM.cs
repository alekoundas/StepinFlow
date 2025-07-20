using Business.Interfaces;
using Business.Services;
using Business.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Enums;
using Model.Models;
using StepinFlow.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class TemplateSearchExecutionVM : ObservableObject, IExecutionViewModel
    {
        private readonly IWindowService _windowService;
        private readonly IDataService _dataService;

        [ObservableProperty]
        private Execution _execution;
        [ObservableProperty]
        private byte[]? _resultImage = null;

        [ObservableProperty]
        private IEnumerable<TemplateMatchModesEnum> _matchModes;

        [ObservableProperty]
        private ObservableCollection<FlowParameter> _flowParameters = new ObservableCollection<FlowParameter>();
        [ObservableProperty]
        private FlowParameter? _selectedFlowParameter = null;


        public TemplateSearchExecutionVM(IWindowService windowService, IDataService dataService)
        {
            _windowService = windowService;
            _dataService = dataService;
            _execution = new Execution();

            MatchModes = Enum.GetValues(typeof(TemplateMatchModesEnum)).Cast<TemplateMatchModesEnum>();
        }

        public async Task SetExecution(Execution execution)
        {
            FlowParameters.Clear();
            ResultImage = null;
            SelectedFlowParameter = null;
            Execution = execution;

            if (execution.ResultImagePath != null)
                if (File.Exists(execution.ResultImagePath))
                    ResultImage = File.ReadAllBytes(execution.ResultImagePath);

            if (execution?.FlowStep?.FlowParameter != null)
            {
                FlowParameters.Add(execution.FlowStep.FlowParameter);
                SelectedFlowParameter = execution.FlowStep.FlowParameter;
            }
            else if (execution?.FlowStep?.FlowParameterId != null)
            {
                FlowParameter selectedFlowParameter = await _dataService.FlowParameters.FirstAsync(x => x.Id == execution.FlowStep.FlowParameterId);
                FlowParameters.Add(selectedFlowParameter);
                SelectedFlowParameter = selectedFlowParameter;
            }
        }

        [RelayCommand]
        private async Task OnTemplateImageDoubleClick(MouseButtonEventArgs e)
        {

            // Check if it's a double-click.
            if (e.ClickCount == 2)
            {
                if (e.Source is System.Windows.Controls.Image img && img.Source is BitmapSource bitmapSource)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                        byte[]? image = await _windowService.OpenScreenshotSelectionWindow(stream.ToArray(), false);
                        if (image != null)
                            using (var stream2 = new MemoryStream(image))
                            {
                                var decoder = BitmapDecoder.Create(stream2, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                img.Source = decoder.Frames[0];
                            }
                    }
                }

            }
        }

        [RelayCommand]
        private async Task OnResultImageDoubleClick(MouseButtonEventArgs e)
        {
            // Check if it's a double-click.
            if (e.ClickCount == 2)
                await _windowService.OpenScreenshotSelectionWindow(ResultImage, false);
        }


        [RelayCommand]
        private void OnImageFailed(object sender)
        {
            //if (sender is Image img)
            //{
            //    img.Source = null; // This prevents the error
            //}
        }
    }
}

