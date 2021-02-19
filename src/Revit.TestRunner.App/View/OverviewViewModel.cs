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
using System.Xml;
using Microsoft.Win32;
using Revit.TestRunner.App.View.TestTreeView;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Client;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.App.View
{
    /// <summary>
    /// ViewModel for the <see cref="OverviewView"/>.
    /// </summary>
    public class OverviewViewModel : AbstractViewModel
    {
        #region Members, Constructor

        private const string ProgramName = "AppRunner";

        private readonly Client mClient;
        private RunnerStatus mRunnerStatus;
        private string mAssemblyPath;
        private string mProgramState;

        /// <summary>
        /// Constructor
        /// </summary>
        public OverviewViewModel()
        {
            mClient = CreateClient();

            Tree = new TreeViewModel();
            Tree.PropertyChanged += ( o, args ) => OnPropertyChangedAll();

            if( !string.IsNullOrEmpty( Properties.Settings.Default.AssemblyPath ) ) {
                LoadAssembly( Properties.Settings.Default.AssemblyPath );
            }

            InstalledRevitVersions = RevitHelper.GetInstalledRevitApplications();
            
            Task.Run( () => mClient.StartRunnerStatusWatcher( CheckStatus, CancellationToken.None ) );
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
            set {
                if( mAssemblyPath != value ) {
                    mAssemblyPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Get the Model Tree of the the assembly.
        /// </summary>
        public TreeViewModel Tree { get; set; }

        /// <summary>
        /// Get detail information of the selected test node.
        /// </summary>
        public string DetailInformation
        {
            get {
                string result = string.Empty;

                if( Tree.SelectedNode != null ) {
                    result = $"{Tree.SelectedNode.Message}\n\n{Tree.SelectedNode.StackTrace}";
                }

                return result;
            }
        }

        /// <summary>
        /// Get the program state of the apüplication.
        /// </summary>
        public string ProgramState
        {
            get => mProgramState;
            set {
                if( mProgramState != value ) {
                    mProgramState = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Get the revit version of the current Revit version, which runs Revit.TestRunner.
        /// </summary>
        public string RevitVersion => IsServerRunning ? mRunnerStatus.RevitVersion : "No Runner available!";

        /// <summary>
        /// Get the log file path of the current running Revit.TestRunner service.
        /// </summary>
        public string LogFilePath => mRunnerStatus?.LogFilePath;

        /// <summary>
        /// Get true, if a Revit.TestRunner service is running.
        /// </summary>
        public bool IsServerRunning => mRunnerStatus != null;
        #endregion

        #region Commands

        public ICommand OpenAssemblyCommand => new AsyncCommand( ExecuteOpenAssemblyCommand );
        /// <summary>
        /// Open assembly for exploring.
        /// </summary>
        private async Task ExecuteOpenAssemblyCommand()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "dll files (*.dll)|*.dll|explore files (*.xml)|*.xml";

            if( dialog.ShowDialog() == true ) {
                if( dialog.FileName.EndsWith( ".xml" ) ) {
                    LoadExploreFile( dialog.FileName );
                }
                else if( dialog.FileName.EndsWith( ".dll" ) ) {
                    await LoadAssembly( dialog.FileName );
                }

                Properties.Settings.Default.AssemblyPath = AssemblyPath;
                Properties.Settings.Default.Save();
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
            var testCases = caseViewModels.Select( ToTestCase );

            int callbackCount = 0;
            TimeSpan duration = TimeSpan.Zero;

            await mClient.StartTestRunAsync( testCases, "2020", result => {
                callbackCount++;
                string points = string.Concat( Enumerable.Repeat( ".", callbackCount % 5 ) );
                ProgramState = "Test Run in progress" + points;
                duration = result.Duration;

                var completed = result.Result.Cases.Where( c => c.State == TestState.Passed || c.State == TestState.Failed ).ToList();

                foreach( TestCase resultCase in completed ) {
                    var caseViewModel = caseViewModels.SingleOrDefault( vm => vm.Id == resultCase.Id );

                    if( caseViewModel != null ) {
                        caseViewModel.State = resultCase.State;
                        caseViewModel.Message = resultCase.Message;
                        caseViewModel.StackTrace = resultCase.StackTrace;
                    }
                }

                if( !string.IsNullOrEmpty( result.Message ) ) ProgramState = result.Message;
            }, CancellationToken.None );

            int total = caseViewModels.Count();
            int passed = caseViewModels.Count( c => c.State == TestState.Passed );
            string message = $"Run finished - duration {duration:g} - {passed} of {total} Tests passed ({Math.Round( 100 * (double)passed / total )}%)";

            ProgramState = message;
            MessageBox.Show( message, "Test run", MessageBoxButton.OK, total == passed ? MessageBoxImage.Information : MessageBoxImage.Error );

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
            var testCases = caseViewModels.Select( ToTestCase );

            RunRequest request = new RunRequest {
                Timestamp = DateTime.Now,
                Cases = testCases.ToArray()
            };

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "json files (*.json)|*.json";

            if( dialog.ShowDialog() == true ) {
                JsonHelper.ToFile( dialog.FileName, request );
            }
        }


        public ICommand OpenWorkDirCommand => new DelegateWpfCommand( ExecuteOpenWorkDirCommand );
        private void ExecuteOpenWorkDirCommand()
        {
            Process.Start( FileNames.WatchDirectory );
        }

        public ICommand OpenLogCommand => new DelegateWpfCommand( ExecuteOpenLogCommand );
        private void ExecuteOpenLogCommand()
        {
            Process.Start( LogFilePath );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Callback method for the client. Get service status.
        /// </summary>
        private void CheckStatus( RunnerStatus status )
        {
            mRunnerStatus = status;
            OnPropertyChangedAll();
        }

        /// <summary>
        /// Create client to send requests to Revit.TestRunner.
        /// </summary>
        private Client CreateClient()
        {
            if( !Directory.Exists( FileNames.WatchDirectory ) ) {
                Directory.CreateDirectory( FileNames.WatchDirectory );
            }

            if( File.Exists( FileNames.RunnerStatusFilePath ) ) {
                FileHelper.DeleteWithLock( FileNames.RunnerStatusFilePath );
            }

            var client = new Client( ProgramName, ProgramVersion );
            return client;
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
            if( !string.IsNullOrEmpty( path ) && File.Exists( path ) ) {
                ProgramState = "Explore file requested...";

                Tree.Clear();

                ExploreResponse response = await mClient.ExploreAssemblyAsync( path, "2020", CancellationToken.None );

                if( response != null ) {
                    LoadExploreFile( response.ExploreFile );

                    if( !string.IsNullOrEmpty( response.Message ) ) {
                        MessageBox.Show( response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                    }
                }
                else {
                    ProgramState = $"Could not load '{path}'";
                }
            }
        }

        /// <summary>
        /// Load a explore response file to show it in the view. 
        /// </summary>
        private void LoadExploreFile( string exploreFile )
        {
            AssemblyPath = string.Empty;

            if( File.Exists( exploreFile ) ) {
                StreamReader xmlStreamReader = new StreamReader( exploreFile );
                XmlDocument xmlDoc = new XmlDocument();

                xmlDoc.Load( xmlStreamReader );
                XmlNode rootNode = (XmlElement)xmlDoc.DocumentElement.FirstChild;

                NUnitTestRun run = new NUnitTestRun( rootNode );
                NodeViewModel root = ToNodeTree( run );

                Tree.AddRootObject( root, true );

                AssemblyPath = root.FullName;

                ProgramState = $"Test Assembly definition loaded '{AssemblyPath}'";
            }
            else {
                ProgramState = $"Explore file not found '{exploreFile}'";
            }
        }

        private NodeViewModel ToNodeTree( NUnitTestRun run )
        {
            NodeViewModel root = new NodeViewModel( run );

            foreach( NUnitTestSuite testSuite in run.TestSuites ) {
                ToNode( root, testSuite );
            }

            return root;
        }

        private void ToNode( NodeViewModel parent, NUnitTestSuite testSuite )
        {
            NodeViewModel node = new NodeViewModel( testSuite );
            parent.Add( node );
            node.Parent = parent;

            foreach( var suite in testSuite.TestSuites ) {
                ToNode( node, suite );
            }

            foreach( var test in testSuite.TestCases ) {
                ToNode( node, test );
            }
        }

        private TestCase ToTestCase( NodeViewModel node )
        {
            if( node == null ) throw new ArgumentNullException();
            if( node.Type != TestType.Case ) throw new ArgumentException( "Only TestCases allowed!" );

            var result = new TestCase {
                Id = node.Id,
                AssemblyPath = AssemblyPath,
                TestClass = node.ClassName,
                MethodName = node.MethodName
            };

            return result;
        }
        #endregion
    }
}
