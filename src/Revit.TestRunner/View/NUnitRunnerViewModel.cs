using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Revit.TestRunner.Runner.NUnit;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Client;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.View.TestTreeView;

namespace Revit.TestRunner.View
{
    public class NUnitRunnerViewModel : DialogViewModel
    {
        #region Members, Constructor

        private readonly RevitTask mRevitTask;
        private string mAssemblyPath;
        private string mProgramState;

        public NUnitRunnerViewModel( RevitTask revitTask )
        {
            InitialHeight = 500;
            InitialWidth = 800;
            DisplayName = "Test Runner";

            mRevitTask = revitTask;

            Tree = new TreeViewModel();
            Tree.PropertyChanged += ( o, args ) => OnPropertyChangedAll();

            if( !string.IsNullOrEmpty( Properties.Settings.Default.AssemblyPath ) ) {
                LoadAssembly( Properties.Settings.Default.AssemblyPath );
            }
        }
        #endregion

        #region Properties

        public string ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public TreeViewModel Tree { get; set; }

        /// <summary>
        /// Filepath of the test assembly.
        /// </summary>
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

        public ICommand OpenLogCommand => new DelegateWpfCommand( () => Process.Start( Log.LogFilePath ), () => File.Exists( Log.LogFilePath ) );

        public ICommand OpenAssemblyCommand => new DelegateWpfCommand( ExecuteOpenAssemblyCommand );

        private void ExecuteOpenAssemblyCommand()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if( dialog.ShowDialog() == true ) {
                LoadAssembly( dialog.FileName );

                Properties.Settings.Default.AssemblyPath = AssemblyPath;
                Properties.Settings.Default.Save();
            }
        }
        #endregion

        #region Start Test Commands

        public ICommand DebugCommand => new AsyncCommand( ExecuteWithReflection, () => Tree.ObjectTree.Any( n => n.IsChecked == true ) );

        public ICommand CreateRequestCommand => new DelegateWpfCommand( CreateRequestFileCommand, () => Tree.ObjectTree.Any( n => n.IsChecked == true ) );
        #endregion

        #region Methods

        private void LoadAssembly( string path )
        {
            if( !string.IsNullOrEmpty( path ) && File.Exists( path ) ) {
                Tree.Clear();

                NUnitRunner runner = new NUnitRunner( path );
                AssemblyPath = runner.TestAssembly;

                runner.ExploreAssembly();

                if( runner.ExploreRun != null ) {
                    NodeViewModel root = ToNodeTree( runner.ExploreRun );

                    Tree.AddRootObject( root, true );
                    runner.Dispose();

                    ProgramState = $"Assembly loaded '{runner.TestAssembly}'";
                }

                runner.Dispose();
            }
        }

        private void CreateRequestFileCommand()
        {
            var caseViewModels = GetSelectedCases();
            var request = CreateRequest( caseViewModels );

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export Test set";
            dialog.InitialDirectory = Properties.Settings.Default.ExportPath;
            dialog.Filter = "Json file (*.json)|*.json";
            dialog.OverwritePrompt = true;

            if( dialog.ShowDialog() == true ) {
                JsonHelper.ToFile( dialog.FileName, request );

                Properties.Settings.Default.ExportPath = dialog.InitialDirectory;
            }
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

        private RunRequest CreateRequest( IEnumerable<NodeViewModel> nodes )
        {
            var testCases = nodes.Select( ToTestCase ).ToList();

            RunRequest request = new RunRequest {
                Id = Client.GenerateId(),
                ClientName = "InRevitRunner",
                ClientVersion = "1.0.0.0",
                Cases = testCases.ToArray()
            };

            return request;
        }

        private async Task ExecuteWithReflection()
        {
            var caseViewModels = GetSelectedCases().ToList();
            var request = CreateRequest( caseViewModels );

            string path = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Revit.TestRunner" );
            var client = new Client( path );

            await client.RunAsync( request, result => {
                var completed = result.Result.Cases.Where( c => c.State == TestState.Passed || c.State == TestState.Failed ).ToList();

                foreach( TestCase resultCase in completed ) {
                    var caseViewModel = caseViewModels.SingleOrDefault( vm => vm.Id == resultCase.Id );

                    if( caseViewModel != null ) {
                        caseViewModel.State = resultCase.State;
                        caseViewModel.Message = resultCase.Message;
                        caseViewModel.StackTrace = resultCase.StackTrace;
                    }
                }
            } );
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
        #endregion
    }
}
