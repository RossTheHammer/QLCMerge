using CommunityToolkit.Mvvm.Input;
using QLCMerge.Common;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace QLCMergeUI.ViewModels
{
    public class MainViewModel : BindableObject
    {
        private const string _pathPlaceholder = "[pick a compatible file]";

        public string LeftFilePath { get; set; } = _pathPlaceholder;

        public string RightFilePath { get; set; } = _pathPlaceholder;

        public ICommand LoadLeftCommand { get; }

        public ICommand LoadRightCommand { get; }

        public ObservableCollection<FunctionDef> LeftFunctions { get; set; } = new ObservableCollection<FunctionDef>();

        public ObservableCollection<FunctionDef> RightFunctions { get; set; } = new ObservableCollection<FunctionDef>();

        public MainViewModel()
        {
            LoadLeftCommand = new AsyncRelayCommand(LoadLeftSource);
            LoadRightCommand = new AsyncRelayCommand(LoadRightSource);
        }

        private async Task LoadLeftSource()
        {
            LeftFilePath = await SelectSourceFile() ?? _pathPlaceholder;
            LoadFunctionDefinitions(LeftFilePath, LeftFunctions);
        }

        private async Task LoadRightSource()
        {
            RightFilePath = await SelectSourceFile() ?? _pathPlaceholder;
            LoadFunctionDefinitions(RightFilePath, RightFunctions);
        }

        private async Task<string?> SelectSourceFile()
        {
            var selected = await FilePicker.Default
                .PickAsync(new PickOptions
                {
                    PickerTitle = "Select a QLC Project File (*.qxw)",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                            {
                                { DevicePlatform.WinUI, new[] { "*.qxw" } }
                            })
                });

            return selected?.FullPath;
        }

        private void LoadFunctionDefinitions(string filePath, ObservableCollection<FunctionDef> functionsList)
        {
            var result = Loader.OpenProjectFile(filePath);

            if (result.IsValid)
            {
                var funcs = Loader.DiscoverFunctions(result.XmlDoc);
                functionsList.Clear();
                foreach (var func in funcs)
                {
                    functionsList.Add(func.Value);
                }
            }
        }
    }
}
