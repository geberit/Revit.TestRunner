using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Revit.TestRunner.Runner.NUnit;
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

        public ICommand DebugCommand => new AsyncCommand( ExecuteWithReflection, () => Tree.SelectedNode != null );
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

        private async Task ExecuteWithReflection()
        {
            Tree.SelectedNode.Descendents.ToList().ForEach( n => n.Reset() );
            Tree.SelectedNode.Reset();


            var caseViewModels = Tree.SelectedNode.Descendents.Where( vm => vm.Type == TestType.Case ).Distinct();
            var testCases = caseViewModels.Select( ToTestCase ).ToList();

            RunRequest request = new RunRequest {
                Id = "someId",
                ClientName = "InRevitRunner",
                ClientVersion = "1.0.0.0",
                Cases = testCases.ToArray()
            };

            string path = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Revit.TestRunner" );
            var client = new Client( path );

            await client.RunAsync( request, result => {
                foreach( TestCase resultCase in result.Result.Cases ) {
                    var caseViewModel = caseViewModels.SingleOrDefault( vm => vm.Id == resultCase.Id );

                    if( caseViewModel != null ) {
                        caseViewModel.State = resultCase.State;
                        caseViewModel.Message = resultCase.Message;
                        caseViewModel.StackTrace = resultCase.StackTrace;
                    }
                }

                if( result.Result.State == TestState.Unknown ) {
                    ProgramState = "Not started";
                }
                else {
                    TimeSpan duration = result.Result.Timestamp - result.Result.StartTime;
                    var passedCases = result.Result.Cases.Where( c => c.State == TestState.Passed );
                    var failedCases = result.Result.Cases.Where( c => c.State == TestState.Failed );
                    int finishedCount = passedCases.Count() + failedCases.Count();


                    if( result.Result.State == TestState.Running ) {
                        ProgramState = $"Running {duration:MMss} - Tests {finishedCount}/{result.Result.Cases.Length}, Failed {failedCases.Count()}";
                    }
                    else {
                        ProgramState = $"Finished {duration:MMss} - Tests {finishedCount}, Failed {failedCases.Count()}";
                        string message = string.Empty;

                        if( result.Result.State == TestState.Passed ) {
                            message = $"Run finished with no errors! TestCount={result.Result.Cases.Length}";
                        }
                        else if( result.Result.State == TestState.Failed ) {
                            message = $"Run finished with errors! TestCount={result.Result.Cases.Length} Failed={result.Result.Cases.Count( c => c.State == TestState.Failed )}";
                        }

                        if( !string.IsNullOrEmpty( message ) ) {
                            Log.Info( message );

                            MessageBox.Show( message,
                                "TestRunner",
                                MessageBoxButton.OK,
                                result.Result.State == TestState.Passed ? MessageBoxImage.Information : MessageBoxImage.Error );
                        }
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
