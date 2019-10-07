using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Revit.TestRunner.Runner.NUnit;

namespace Revit.TestRunner.View.TestTreeView
{
    /// <summary>
    /// Hierarchic ViewModel
    /// </summary>
    public class NodeViewModel : AbstractViewModel
    {
        #region Members, Constructor

        private readonly NUnitResult mNUnitResult;
        private bool mIsExpanded;
        private TestState mState;
        private string mMessage;
        private string mStackTrace;

        internal NodeViewModel( NUnitResult nUnitResult )
        {
            // Tree Stuff
            Children = new List<NodeViewModel>();
            IsExpanded = true;
            IsShow = true;

            // Test Stuff
            mNUnitResult = nUnitResult ?? throw new System.ArgumentNullException( nameof( nUnitResult ) );

            State = mNUnitResult.Result;
            Message = mNUnitResult.Message;
            StackTrace = mNUnitResult.FailureStackTrace;

        }

        #endregion

        #region Test Properties

        public string Text
        {
            get
            {
                string result = string.Empty;

                if( Type == TestType.Run ) result = "Test Run";
                else if( Type != TestType.Unknown ) result = mNUnitResult.Name;

                return result;
            }
        }

        public string ToolTip => Path;

        public string Path => Text + "/" + string.Join( "/", Ancestors.Select( a => a.Text ) );

        public string FullName => mNUnitResult.FullName;

        public TestType Type => mNUnitResult.Type;

        public TestState State
        {
            get
            {
                TestState result = TestState.Unknown;

                if( Children.Count == 0 ) result = mState;
                else {
                    if( Children.Any( c => c.State == TestState.Passed ) ) result = TestState.Passed;
                    if( Children.Any( c => c.State == TestState.Failed ) ) result = TestState.Failed;
                }

                return result;
            }
            set
            {
                if( Children.Count == 0 ) {
                    if( value == mState ) return;
                    mState = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Id => mNUnitResult.Id;

        public string ClassName => mNUnitResult.ClassName;

        public string MethodName => mNUnitResult.MethodName;

        public string Message
        {
            get =>
                Children.Count == 0
                    ? mMessage
                    : State == TestState.Failed
                        ? "One ore more child tests had errors."
                        : string.Empty;
            set
            {
                if( Children.Count == 0 ) {
                    if( value == mMessage ) return;
                    mMessage = value;
                    OnPropertyChanged( () => Message );
                }
            }
        }

        public string StackTrace
        {
            get => mStackTrace;
            set
            {
                if( value == mStackTrace ) return;
                mStackTrace = value;
                OnPropertyChanged( () => StackTrace );
            }
        }

        #endregion


        #region Tree Stuff - do not change
        #region Tree Properties

        public NodeViewModel Parent { get; set; }

        private List<NodeViewModel> Children { get; }

        private IEnumerable<NodeViewModel> Ancestors
        {
            get
            {
                var result = new List<NodeViewModel>();

                if( Parent != null ) {
                    result.Add( Parent );
                    result.AddRange( Parent.Ancestors );
                }

                return result;
            }
        }

        internal IEnumerable<NodeViewModel> Descendents
        {
            get
            {
                var result = new List<NodeViewModel>();

                foreach( NodeViewModel child in Children ) {
                    result.Add( child );
                    result.AddRange( child.Descendents );
                }

                return result;
            }
        }

        public bool IsExpanded
        {
            get => mIsExpanded;
            set
            {
                if( value != mIsExpanded ) {
                    mIsExpanded = value;

                    foreach( NodeViewModel node in Descendents ) {
                        node.IsShow = value;
                    }

                    OnPropertyChanged();
                }
            }
        }

        public bool IsShow { get; private set; }

        public bool ShowExpandButton => Children.Any();

        public Thickness Margin => new Thickness( GetDeep( this ) * 15, 0, 0, 0 );

        #endregion

        #region Tree Methods

        internal void Add( NodeViewModel child )
        {
            Children.Add( child );
            child.PropertyChanged += OnNodePropertyChanged;
        }

        internal void Remove( NodeViewModel child )
        {
            Children.Remove( child );
            child.PropertyChanged += OnNodePropertyChanged;
        }

        private static int GetDeep( NodeViewModel viewModel, int deep = 0 )
        {
            int result = deep;  // root level -> 0

            if( viewModel.Parent != null ) {
                result = GetDeep( viewModel.Parent, deep + 1 );
            }

            return result;
        }

        private void OnNodePropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            OnPropertyChanged( e );
        }

        internal void Reset()
        {
            State = TestState.Unknown;
            Message = string.Empty;
            StackTrace = string.Empty;
        }

        public override string ToString()
        {
            const string offset = "  ";
            string result = string.Empty;

            for( int i = 0; i < GetDeep( this ); i++ ) {
                result += offset;
            }

            result += $"{Text} [{Text}]";

            return result;
        }

        #endregion 
        #endregion
    }
}
