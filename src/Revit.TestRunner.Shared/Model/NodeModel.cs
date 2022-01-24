using System.Collections.Generic;
using System.Linq;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.Shared.Model
{
    /// <summary>
    /// Hierarchic Test Model
    /// </summary>
    public class NodeModel : AbstractNotifyPropertyChanged
    {
        #region Members, Constructor

        private readonly NUnitResult mNUnitResult;
        private TestState mState;
        private string mMessage;
        private string mStackTrace;

        internal NodeModel( NUnitResult nUnitResult )
        {
            // Tree Stuff
            Children = new List<NodeModel>();

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
            get {
                string result = string.Empty;

                if( Type == TestType.Run ) result = "Test Run";
                else if( Type != TestType.Unknown ) result = mNUnitResult.Name;

                return result;
            }
        }

        public string Path => Text + "/" + string.Join( "/", Ancestors.Select( a => a.Text ) );

        public string FullName => mNUnitResult.FullName;

        public TestType Type => mNUnitResult.Type;

        public TestState State
        {
            get {
                TestState result = TestState.Unknown;

                if( Children.Count == 0 ) result = mState;
                else {
                    var cases = Descendents.Where( n => n.Type == TestType.Case );

                    if( cases.Any( c => c.State == TestState.Failed ) ) {
                        result = TestState.Failed;
                    }
                    else if( cases.Where( c => c.State != TestState.Explicit && c.State != TestState.Ignore ).All( c => c.State == TestState.Passed ) ) {
                        result = TestState.Passed;
                    }
                }

                return result;
            }
            set {
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
                        ? "One or more child tests had errors."
                        : string.Empty;
            set {
                if( Children.Count == 0 ) {
                    if( value == mMessage ) return;
                    mMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StackTrace
        {
            get => mStackTrace;
            set {
                if( value == mStackTrace ) return;
                mStackTrace = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region Tree Stuff - do not change
        #region Tree Properties

        public NodeModel Root => Parent != null ? Parent.Root : this;

        public NodeModel Parent { get; set; }

        public List<NodeModel> Children { get; }

        private IEnumerable<NodeModel> Ancestors
        {
            get {
                var result = new List<NodeModel>();

                if( Parent != null ) {
                    result.Add( Parent );
                    result.AddRange( Parent.Ancestors );
                }

                return result;
            }
        }

        public IEnumerable<NodeModel> DescendantsAndMe => Descendents.ToList().Append( this );

        private IEnumerable<NodeModel> Descendents
        {
            get {
                var result = new List<NodeModel>();

                foreach( NodeModel child in Children ) {
                    result.Add( child );
                    result.AddRange( child.Descendents );
                }

                return result;
            }
        }

        #endregion

        #region Tree Methods

        internal void Add( NodeModel child )
        {
            Children.Add( child );
            child.PropertyChanged += OnNodePropertyChanged;
        }

        private static int GetDeep( NodeModel viewModel, int deep = 0 )
        {
            int result = deep;  // root level -> 0

            if( viewModel.Parent != null ) {
                result = GetDeep( viewModel.Parent, deep + 1 );
            }

            return result;
        }

        private void OnNodePropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            //OnPropertyChanged( e );
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
