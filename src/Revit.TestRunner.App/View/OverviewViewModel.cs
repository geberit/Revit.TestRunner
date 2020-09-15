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
    public class OverviewViewModel : AbstractViewModel
    {
        #region Members, Constructor

        private string mAssemblyPath;
        private string mProgramState;

        public OverviewViewModel()
        {
            Tree = new TreeViewModel();
            Tree.PropertyChanged += ( o, args ) => OnPropertyChangedAll();

            if( !string.IsNullOrEmpty( Properties.Settings.Default.AssemblyPath ) ) {
                LoadAssembly( Properties.Settings.Default.AssemblyPath );
            }
        }
        #endregion

        #region Properties

        public string ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private const string ProgramName = "AppRunner";

        public TreeViewModel Tree { get; set; }

        public string AssemblyPath
        {
            get => mAssemblyPath;
            set {
                if( value == mAssemblyPath ) return;
                mAssemblyPath = value;
                OnPropertyChanged( () => AssemblyPath );
            }
        }

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

        public string ProgramState
        {
            get => mProgramState;
            set {
                if( value == mProgramState ) return;
                mProgramState = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand OpenAssemblyCommand => new AsyncCommand( ExecuteOpenAssemblyCommand );
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
        private async Task ExecuteRunCommand()
        {
            ProgramState = "Test Run in started";

            var caseViewModels = GetSelectedCases().ToList();
            var testCases = caseViewModels.Select( ToTestCase );

            var client = GetClient();
            int callbackCount = 0;
            TimeSpan duration = TimeSpan.Zero;

            await client.StartTestRunAsync( testCases, result => {
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
            }, CancellationToken.None );

            int total = caseViewModels.Count();
            int passed = caseViewModels.Count( c => c.State == TestState.Passed );
            string message = $"Run finished - duration {duration:g} - {passed} of {total} Tests passed ({Math.Round( 100 * (double)passed / total )}%)";

            ProgramState = message;
            MessageBox.Show( message, "Test run", MessageBoxButton.OK, total == passed ? MessageBoxImage.Information : MessageBoxImage.Error );
        }

        public ICommand CreateRequestCommand => new DelegateWpfCommand( ExecuteCreateRequestCommand, () => Tree.HasObjects );
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

        #endregion

        #region Methods

        private Client GetClient()
        {
            var client = new Client( ProgramName, ProgramVersion );
            return client;
        }

        private IEnumerable<NodeViewModel> GetSelectedCases()
        {
            var checkedNodes = Tree.ObjectTree.Where( node => node.IsChecked == true ).ToList();
            checkedNodes.ForEach( n => n.Reset() );

            var viewModels = checkedNodes.SelectMany( node => node.Descendents ).ToList();
            checkedNodes.ForEach( n => viewModels.Add( n ) );

            var caseViewModels = viewModels.Where( vm => vm.Type == TestType.Case ).ToList().Distinct();

            return caseViewModels;
        }

        private async Task LoadAssembly( string path )
        {
            if( !string.IsNullOrEmpty( path ) && File.Exists( path ) ) {
                ProgramState = "Explore file requested...";

                Tree.Clear();

                var client = GetClient();
                ExploreResponse response = await client.ExploreAssemblyAsync( path, CancellationToken.None );

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
