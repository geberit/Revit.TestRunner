using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Revit.TestRunner.Runner.Direct;
using Revit.TestRunner.Runner.NUnit;
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
            set
            {
                if( value == mAssemblyPath ) return;
                mAssemblyPath = value;
                OnPropertyChanged( () => AssemblyPath );
            }
        }

        public string DetailInformation
        {
            get
            {
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
            set
            {
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
            DateTime start = DateTime.Now;
            ProgramState = "Test Run started...";

            Tree.SelectedNode.Descendents.ToList().ForEach( n => n.Reset() );
            Tree.SelectedNode.Reset();

            var toRun = CasesToRun( Tree.SelectedNode ).ToList();

            ReflectionRunner runner = new ReflectionRunner( AssemblyPath );

            await mRevitTask.Run( async application => {
                for( int i = 0; i < toRun.Count; i++ ) {
                    ProgramState = $"Run Test {i + 1} of {toRun.Count}";
                    await runner.RunTest( toRun[i], application );
                }
            } );

            PresentResults( toRun, start );
        }

        private IEnumerable<NodeViewModel> CasesToRun( NodeViewModel root )
        {
            var list = new List<NodeViewModel>( root.Descendents ) { root };
            var cases = list.Where( n => n.Type == TestType.Case ).ToList();

            if( root.Type == TestType.Case ) cases.Add( root );

            var toRun = cases.Distinct().ToList();
            return toRun;
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


        private void PresentResults( IEnumerable<NodeViewModel> tests, DateTime start )
        {
            DateTime end = DateTime.Now;
            bool success = tests.All( n => n.State == TestState.Passed );

            var cases = tests.Where( t => t.Type == TestType.Case );
            int passed = cases.Count( t => t.State == TestState.Passed );
            int failed = cases.Count( t => t.State == TestState.Failed );
            int unknown = cases.Count( t => t.State == TestState.Unknown );

            ProgramState = $"Test Run finished at {end:T}. Passed {passed} of {cases.Count()}";

            string message = $"Run finished at {end:T}\n\n" +
                             $"Passed Tests {passed} of {cases.Count()}\n" +
                             $"Run Duration {end - start}";

            Log.Info( message );

            //MessageBox.Show( message,
            //    "TestRunner",
            //    MessageBoxButton.OK,
            //    success ? MessageBoxImage.Information : MessageBoxImage.Error );
        }
        #endregion
    }
}
