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

        public ObservableCollection<FixtureDef> LeftFixtures { get; set; } = new ObservableCollection<FixtureDef>();
        public ObservableCollection<FunctionDef> LeftFunctions { get; set; } = new ObservableCollection<FunctionDef>();

        public ObservableCollection<FixtureDef> RightFixtures { get; set; } = new ObservableCollection<FixtureDef>();
        public ObservableCollection<FunctionDef> RightFunctions { get; set; } = new ObservableCollection<FunctionDef>();

        public MainViewModel()
        {
            LoadLeftCommand = new AsyncRelayCommand(LoadLeftSource);
            LoadRightCommand = new AsyncRelayCommand(LoadRightSource);
        }

        private async Task LoadLeftSource()
        {
            LeftFilePath = await SelectSourceFile() ?? _pathPlaceholder;
            LoadDefinitions(LeftFilePath, LeftFixtures, LeftFunctions);
        }

        private async Task LoadRightSource()
        {
            RightFilePath = await SelectSourceFile() ?? _pathPlaceholder;
            LoadDefinitions(RightFilePath, RightFixtures, RightFunctions);
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

        private void LoadDefinitions(string filePath, ObservableCollection<FixtureDef> fixturesList, ObservableCollection<FunctionDef> functionsList)
        {
            var result = Loader.OpenProjectFile(filePath);

            if (result.IsValid)
            {
                var fixtures = Loader.DiscoverFixtures(result.XmlDoc);
                fixturesList.Clear();
                foreach (var fixture in fixtures)
                {
                    fixturesList.Add(fixture.Value);
                }

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
