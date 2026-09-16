using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QLCMerge.Common;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Windows.ApplicationModel.Activation;

namespace QLCMergeUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const string _pathPlaceholder = "[pick a compatible file]";

        private string _leftFilePath = _pathPlaceholder;
        private string _rightFilePath = _pathPlaceholder;
        private bool _fixturesMatch = false;
        private bool _functionsMatch = false;
        private int? _divergentId = null;
        private int _leftDivergentCount = 0;
        private int _rightDivergentCount = 0;

        public string LeftFilePath
        {
            get => _leftFilePath;
            set => SetProperty(ref _leftFilePath, value);
        }

        public string RightFilePath
        {
            get => _rightFilePath;
            set => SetProperty(ref _rightFilePath, value);
        }

        public bool FixturesMatch
        {
            get => _fixturesMatch;
            set => SetProperty(ref _fixturesMatch, value);
        }

        public bool FunctionsMatch
        {
            get => _functionsMatch;
            set => SetProperty(ref _functionsMatch, value);
        }

        public int? DivergentId
        {
            get => _divergentId;
            set => SetProperty(ref _divergentId, value);
        }

        public int LeftDivergentCount
        {
            get => _leftDivergentCount;
            set => SetProperty(ref _leftDivergentCount, value);
        }

        public int RightDivergentCount
        {
            get => _rightDivergentCount;
            set => SetProperty(ref _rightDivergentCount, value);
        }

        public ObservableCollection<FixtureDef> LeftFixtures { get; } //= new ObservableCollection<FixtureDef>();
        public ObservableCollection<FunctionDef> LeftFunctions { get; } //= new ObservableCollection<FunctionDef>();
        public ObservableCollection<FunctionDef> SelectedLeftFunctions { get; } //= new ObservableCollection<FunctionDef>();
        public ICommand LeftSelectionChangedCommand { get; }

        public ObservableCollection<FixtureDef> RightFixtures { get; } //= new ObservableCollection<FixtureDef>();
        public ObservableCollection<FunctionDef> RightFunctions { get; } //= new ObservableCollection<FunctionDef>();
        public ObservableCollection<FunctionDef> SelectedRightFunctions { get; } //= new ObservableCollection<FunctionDef>();
        public ICommand RightSelectionChangedCommand { get; }


        public ICommand LoadLeftCommand { get; }
        public ICommand LoadRightCommand { get; }

        public ICommand SelectLeftDivergentCommand { get; }
        public ICommand SelectRightDivergentCommand { get; }

        public ICommand CopyLeftToRightCommand { get; }
        public ICommand CopyRightToLeftCommand { get; }

        public MainViewModel() : base()
        {
            LeftFixtures = new ObservableCollection<FixtureDef>();
            LeftFunctions = new ObservableCollection<FunctionDef>();
            SelectedLeftFunctions = new ObservableCollection<FunctionDef>();
            LeftSelectionChangedCommand = new AsyncRelayCommand<object>(LeftSelectionChanged);

            SelectedLeftFunctions.CollectionChanged += SelectedFunctions_CollectionChanged;

            RightFixtures = new ObservableCollection<FixtureDef>();
            RightFunctions = new ObservableCollection<FunctionDef>();
            SelectedRightFunctions = new ObservableCollection<FunctionDef>();
            RightSelectionChangedCommand = new AsyncRelayCommand<object>(RightSelectionChanged);

            SelectedRightFunctions.CollectionChanged += SelectedFunctions_CollectionChanged;

            LoadLeftCommand = new AsyncRelayCommand(LoadLeftSource);
            LoadRightCommand = new AsyncRelayCommand(LoadRightSource);

            SelectLeftDivergentCommand = new AsyncRelayCommand(SelectLeftDivergent);
            SelectRightDivergentCommand = new AsyncRelayCommand(SelectRightDivergent);

            CopyLeftToRightCommand = new AsyncRelayCommand(CopyLeftToRight);
            CopyRightToLeftCommand = new AsyncRelayCommand(CopyRightToLeft);
        }

        private async Task LeftSelectionChanged(object? p)
        {
            Debug.WriteLine(p?.GetType().Name);
        }

        private async Task RightSelectionChanged(object? p)
        {
            Debug.WriteLine(p?.GetType().Name);
        }

        private void SelectedFunctions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine(nameof(SelectedFunctions_CollectionChanged));
        }

        private async Task CopyLeftToRight()
        {
            CopyFromTo(LeftFunctions, SelectedLeftFunctions, RightFunctions);
        }

        private async Task CopyRightToLeft()
        {
            CopyFromTo(RightFunctions, SelectedRightFunctions, LeftFunctions);
        }

        private void CopyFromTo(
            ObservableCollection<FunctionDef> from, 
            ObservableCollection<FunctionDef> selected, 
            ObservableCollection<FunctionDef> to)
        {
            // next open ID
            var nextId = to.Max(f => f.Id).GetValueOrDefault() + 1;

            // create an ID map
            var idMap = new Dictionary<int, int>();
            foreach (var func in selected)
            {
                if (func.Id.HasValue)
                {
                    func.MapTo = nextId++;
                    idMap.Add(func.Id.Value, func.MapTo.Value);
                }
            }



            // TODO - what about Modified that reference the divergent ID?
        }

        private async Task SelectLeftDivergent()
        {
            foreach (var func in LeftFunctions)
            {
                var selected = SelectedLeftFunctions.FirstOrDefault(f => f.Id == func.Id);
                if (func.Matched == DefinitionMatchType.Divergent)
                {
                    if(selected == null)
                    {
                        SelectedLeftFunctions.Add(func);
                    }
                } else if (selected != null)
                {
                    SelectedLeftFunctions.Remove(selected);
                }
            }
        }

        private async Task SelectRightDivergent()
        {
            foreach (var func in RightFunctions)
            {
                var selected = SelectedRightFunctions.FirstOrDefault(f => f.Id == func.Id);
                if (func.Matched == DefinitionMatchType.Divergent)
                {
                    if (selected == null)
                    {
                        SelectedRightFunctions.Add(func);
                    }
                }
                else if (selected != null)
                {
                    SelectedRightFunctions.Remove(selected);
                }
            }
        }

        private Dictionary<string,FunctionDef> AddTreeBase(ObservableCollection<FunctionDef> funcCollection)
        {
            var quickRef = new Dictionary<string, FunctionDef>();
            foreach (var supportedType in Loader.SupportedFunctionTypes)
            {
                funcCollection.Add(new FunctionDef("Folder", supportedType));
                quickRef.Add(supportedType, funcCollection.First(f => f.Name == supportedType));
            }
            return quickRef; 
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
                
                foreach(var func in funcs)
                {
                    functionsList.Add(func.Value);
                }
            }

            FixturesMatch = DoFixturesMatch();
            var functionComparison = CompareFunctions();
            FunctionsMatch = functionComparison.synced;
            DivergentId = functionComparison.synced ? null : functionComparison.divergentId;
            LeftDivergentCount = LeftFunctions.Count(f => f.Matched == DefinitionMatchType.Divergent);
            RightDivergentCount = RightFunctions.Count(f => f.Matched == DefinitionMatchType.Divergent);
        }

        private bool DoFixturesMatch()
        {
            if(LeftFixtures.Count !=  RightFixtures.Count)
            {  return false; }

            for (var index = 0; index < LeftFixtures.Count; index++)
            {
                if (LeftFixtures[index].Id != RightFixtures[index].Id) 
                { return false; }

                if (LeftFixtures[index].Address != RightFixtures[index].Address)
                { return false; }

                if (LeftFixtures[index].Name != RightFixtures[index].Name)
                { return false; }

                if (LeftFixtures[index].Channels != RightFixtures[index].Channels)
                { return false; }
            }

            return true;
        }

        private (bool synced, int divergentId) CompareFunctions()
        {
            if(LeftFunctions.Count == 0 || RightFunctions.Count == 0) { return (false,0); }

            var maxLeftId = LeftFunctions.Select(f => f.Id).Max();
            var maxRightId = RightFunctions.Select(f => f.Id).Max();
            if (maxLeftId == null || maxRightId == null) { return (false, 0); }

            var maxId = Math.Max(maxLeftId.Value, maxRightId.Value);
            var divergedAt = maxId + 1;

            for (var id = 0; id < maxId; id++) {

                var leftDef = id <= maxLeftId ? LeftFunctions.FirstOrDefault(f => f.Id == id) : null;
                var rightDef = id <= maxRightId ? RightFunctions.FirstOrDefault(f => f.Id == id) : null;

                if (leftDef == null && rightDef == null)
                {
                    // neither has this ID, move on
                }
                else if (leftDef == null && rightDef != null)
                {
                    rightDef.Matched = DefinitionMatchType.None;
                }
                else if (leftDef != null && rightDef == null)
                {
                    leftDef.Matched = DefinitionMatchType.None;
                }
                else if (leftDef.ElemType == rightDef.ElemType
                    && leftDef.Name == rightDef.Name 
                    && leftDef.Inner == rightDef.Inner)
                {
                    leftDef.Matched = DefinitionMatchType.Matched;
                    rightDef.Matched = DefinitionMatchType.Matched;
                }
                else
                {
                    // ID matches, check properties
                    if (leftDef.ElemType != rightDef.ElemType)
                    {
                        leftDef.Matched = DefinitionMatchType.Divergent;
                        rightDef.Matched = DefinitionMatchType.Divergent;
                        if (divergedAt > id) 
                        { 
                            divergedAt = id; 
                        }
                    }
                    else if (leftDef.Name != rightDef.Name && leftDef.Inner == rightDef.Inner)
                    {
                        // name change only
                        leftDef.Matched = DefinitionMatchType.NameChange;
                        rightDef.Matched = DefinitionMatchType.NameChange;
                    }
                    else if (leftDef.Inner != rightDef.Inner && leftDef.Name == rightDef.Name)
                    {
                        // same function, but has been modified
                        leftDef.Matched = DefinitionMatchType.Modified;
                        rightDef.Matched = DefinitionMatchType.Modified;
                    }
                    else
                    {
                        leftDef.Matched = DefinitionMatchType.Divergent;
                        rightDef.Matched = DefinitionMatchType.Divergent;
                        if (divergedAt > id)
                        {
                            divergedAt = id;
                        }
                    }
                }

                //if (leftDef.Id > rightDef.Id)
                //{
                //    var found = LeftFunctions.Where(f => f.Name == rightDef.Name && f.ElemType == rightDef.ElemType);
                //    if (found.Any()) {
                //        rightDef.MapTo = found.First().Id;
                //    }
                //    rightOffset++;
                //} 
                //else if (leftDef.Id < rightDef.Id)
                //{
                //    var found = RightFunctions.Where(f => f.Name == leftDef.Name && f.ElemType == leftDef.ElemType);
                //    if (found.Any())
                //    {
                //        leftDef.MapTo = found.First().Id;
                //    }
                //    leftOffset++;
                //} 
                //else
                //{
                //}
            }

            var synced = divergedAt > maxId;
            return (synced, divergedAt);
        }
    }
}
