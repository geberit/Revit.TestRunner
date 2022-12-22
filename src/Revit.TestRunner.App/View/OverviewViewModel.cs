using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Revit.TestRunner.App.View.TestTreeView;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Dto;
using Revit.TestRunner.Shared.Model;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.App.View
{
    /// <summary>
    /// ViewModel for the <see cref="OverviewView"/>.
    /// </summary>
    public class OverviewViewModel : AbstractNotifyPropertyChanged
    {
        #region Members, Constructor

        private const string ProgramName = "AppRunner";

        private readonly TestRunnerClient mClient;
        private HomeDto mHomeDto;
        private string mAssemblyPath;
        private string mProgramState;
        private bool mIsLoading;

        /// <summary>
        /// Constructor
        /// </summary>
        public OverviewViewModel()
        {
            mClient = new TestRunnerClient( ProgramName, ProgramVersion );

            Tree = new TreeViewModel();
            Tree.PropertyChanged += ( o, args ) => OnPropertyChangedAll();

            InstalledRevitVersions = RevitHelper.GetInstalledRevitApplications();

            mClient.StartRunnerStatusWatcher( CheckHome, CancellationToken.None );

            Recent = Properties.Settings.Default.AssemblyPath?.Trim( ';' ).Split( ';' ) ?? Enumerable.Empty<string>();
            AssemblyPath = Recent.FirstOrDefault();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get all installed Revit applications on this host.
        /// </summary>
        public IEnumerable<string> InstalledRevitVersions { get; }

        /// <summary>
        /// Get the program version of Revit.TestRunner.
        /// </summary>
        public string ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Get the test assembly path.
        /// </summary>
        public string AssemblyPath
        {
            get => mAssemblyPath;
            set
            {
                if( mAssemblyPath != value ) {
                    mAssemblyPath = value;
                    OnPropertyChanged();

                    if( !string.IsNullOrEmpty( value ) ) {
                        _ = LoadAssembly( value );
                    }
                }
            }
        }

        /// <summary>
        /// Recent loaded assemblies. max count = 10.
        /// </summary>
        public IEnumerable<string> Recent
        {
            get => Properties.Settings.Default.AssemblyPath.Split( ';' );
            private set
            {
                var list = value != null ? value.Take( 10 ) : Enumerable.Empty<string>();
                Properties.Settings.Default.AssemblyPath = string.Join( ";", list );
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get the Model Tree of the the assembly.
        /// </summary>
        public TreeViewModel Tree { get; }

        /// <summary>
        /// Get true if a node is selected
        /// </summary>
        public bool IsNodeSelected => Tree.SelectedNode != null;

        /// <summary>
        /// Get the program state of the apüplication.
        /// </summary>
        public string ProgramState
        {
            get => mProgramState;
            set
            {
                if( mProgramState != value ) {
                    mProgramState = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Get the revit version of the current Revit version, which runs Revit.TestRunner.
        /// </summary>
        public string RevitVersion => IsServerRunning ? mHomeDto.RevitVersion : "No Runner available!";

        /// <summary>
        /// Get the log file path of the current running Revit.TestRunner service.
        /// </summary>
        public string LogFilePath => mHomeDto?.LogFilePath;

        /// <summary>
        /// Get true, if a Revit.TestRunner service is running.
        /// </summary>
        public bool IsServerRunning => mHomeDto != null;

        #endregion

        #region Commands

        public ICommand OpenAssemblyCommand => new DelegateWpfCommand( ExecuteOpenAssemblyCommand );

        /// <summary>
        /// Open assembly for exploring.
        /// </summary>
        private void ExecuteOpenAssemblyCommand()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "dll files (*.dll)|*.dll|explore files (*.xml)|*.xml";

            if( dialog.ShowDialog() == true ) {
                if( dialog.FileName.EndsWith( ".xml" ) ) {
                    LoadExploreFile( dialog.FileName );
                }
                else if( dialog.FileName.EndsWith( ".dll" ) ) {
                    AssemblyPath = dialog.FileName;
                }
            }
        }

        public ICommand RunCommand => new AsyncCommand( ExecuteRunCommand, () => Tree.HasObjects );

        /// <summary>
        /// Execute selected tests on Revit.TestRunner.
        /// </summary>
        private async Task ExecuteRunCommand()
        {
            ProgramState = "Test Run in started";

            var caseViewModels = GetSelectedCases().ToList();
            var testCases = caseViewModels.Select( vm => ModelHelper.ToTestCase( vm.Model ) );

            int callbackCount = 0;
            TimeSpan duration = TimeSpan.Zero;

            await mClient.StartTestRunAsync( testCases, "2020", "", result => {
                callbackCount++;
                string points = string.Concat( Enumerable.Repeat( ".", callbackCount % 5 ) );
                ProgramState = "Test Run in progress" + points;
                duration = result.Duration;

                if( result?.StateDto != null ) {
                    var completed = result.StateDto.Cases.Where( c => c.State != TestState.Unknown );

                    foreach( TestCaseDto resultCase in completed.ToList() ) {
                        var caseViewModel = caseViewModels.SingleOrDefault( vm => vm.Id == resultCase.Id );

                        if( caseViewModel != null ) {
                            caseViewModel.State = resultCase.State;
                            caseViewModel.Message = resultCase.Message;
                            caseViewModel.StackTrace = resultCase.StackTrace;
                            caseViewModel.StartTime = resultCase.StartTime;
                            caseViewModel.EndTime = resultCase.EndTime;
                        }
                    }
                }

                if( !string.IsNullOrEmpty( result.Message ) ) ProgramState = result.Message;
            }, CancellationToken.None );

            int total = caseViewModels.Count( c => c.State == TestState.Passed || c.State == TestState.Failed );
            int passed = caseViewModels.Count( c => c.State == TestState.Passed );
            bool success = total == passed;
            string successText = success ? "Run finished successfully" : "Run ended with errors";

            string message = $"{successText}\n\nDuration: {duration:hh\\:mm\\:ss\\.fff}\nTests passed: {passed} of {total} ({Math.Round( 100 * (double)passed / total )}%)";

            ProgramState = message.Replace( "\n\n", "\n" ).Replace( "\n", " - " );
            MessageBox.Show( message, "Test run", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error );

            foreach( NodeViewModel node in Tree.ObjectTree ) {
                node.IsChecked = false;
            }
        }

        public ICommand CreateRequestCommand => new DelegateWpfCommand( ExecuteCreateRequestCommand, () => Tree.HasObjects );

        /// <summary>
        /// Create a request file for selected tests.
        /// </summary>
        private void ExecuteCreateRequestCommand()
        {
            var caseViewModels = GetSelectedCases().ToList();
            var testCases = caseViewModels.Select( vm => ModelHelper.ToTestCase( vm.Model ) );

            TestRequestDto request = new TestRequestDto {
                Timestamp = DateTime.Now,
                Cases = testCases.ToArray()
            };

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "json files (*.json)|*.json";

            if( dialog.ShowDialog() == true ) {
                JsonHelper.ToFile( dialog.FileName, request );
            }
        }

        public ICommand RefreshCommand => new DelegateWpfCommand( RefreshExecute );

        private void RefreshExecute()
        {
            if( !string.IsNullOrEmpty( AssemblyPath ) ) {
                var path = AssemblyPath;
                AssemblyPath = string.Empty;
                AssemblyPath = path;
            }
        }

        public ICommand OpenWorkDirCommand => new DelegateWpfCommand( ExecuteOpenWorkDirCommand );

        private void ExecuteOpenWorkDirCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                Arguments = FileNames.WatchDirectory,
                FileName = "explorer.exe"
            };

            Process.Start( startInfo );
        }

        public ICommand OpenLogCommand => new DelegateWpfCommand( ExecuteOpenLogCommand );

        private void ExecuteOpenLogCommand()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo( LogFilePath ) {
                UseShellExecute = true
            };

            Process.Start( startInfo );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Callback method for the client. Get service status.
        /// </summary>
        private void CheckHome( HomeDto home )
        {
            mHomeDto = home;
            OnPropertyChangedAll();
        }

        /// <summary>
        /// Get selected test cases in a list.
        /// </summary>
        private IEnumerable<NodeViewModel> GetSelectedCases()
        {
            var checkedNodes = Tree.ObjectTree.Where( node => node.IsChecked == true ).ToList();
            checkedNodes.ForEach( n => n.Reset() );

            var viewModels = checkedNodes.SelectMany( node => node.Descendents ).ToList();
            checkedNodes.ForEach( n => viewModels.Add( n ) );

            var caseViewModels = viewModels.Where( vm => vm.Type == TestType.Case ).ToList().Distinct();

            return caseViewModels;
        }

        /// <summary>
        /// Start a explore request for the desired test assembly. 
        /// </summary>
        private async Task LoadAssembly( string path )
        {
            if( !mIsLoading ) {
                var recentList = Recent.ToList();
                if( recentList.Contains( path ) ) recentList.Remove( path );

                if( !string.IsNullOrEmpty( path ) && File.Exists( path ) ) {
                    ProgramState = "Explore file requested...";

                    mIsLoading = true;
                    Tree.Clear();

                    ExploreResponseDto response = await mClient.ExploreAssemblyAsync( path, "2020", "", CancellationToken.None );

                    if( response != null ) {
                        LoadExploreFile( response.ExploreFile );
                        recentList.Insert( 0, path );

                        if( !string.IsNullOrEmpty( response.Message ) ) {
                            MessageBox.Show( response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                        }
                    }
                    else {
                        ProgramState = $"Could not load '{path}'";
                    }

                    mIsLoading = false;
                }

                Recent = recentList;
            }
        }

        /// <summary>
        /// Load a explore response file to show it in the view. 
        /// </summary>
        private void LoadExploreFile( string exploreFile )
        {
            AssemblyPath = string.Empty;

            if( File.Exists( exploreFile ) ) {
                var rootModel = ModelHelper.ToNodeTree( exploreFile );

                NodeViewModel root = ToNodeTree( rootModel );
                Tree.AddRootObject( root, true );
                Tree.SelectedNode = null;

                AssemblyPath = root.FullName;

                ProgramState = $"Test Assembly definition loaded '{AssemblyPath}'";
            }
            else {
                ProgramState = $"Explore file not found '{exploreFile}'";
            }
        }

        private NodeViewModel ToNodeTree( NodeModel run )
        {
            NodeViewModel root = new NodeViewModel( run );

            foreach( NodeModel testSuite in run.Children ) {
                ToNode( root, testSuite );
            }

            return root;
        }

        private void ToNode( NodeViewModel parent, NodeModel testSuite )
        {
            NodeViewModel node = new NodeViewModel( testSuite );
            parent.Add( node );
            node.Parent = parent;

            foreach( var child in testSuite.Children ) {
                ToNode( node, child );
            }
        }

        #endregion
    }
}