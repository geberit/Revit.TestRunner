using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Model;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.App.View.TestTreeView
{
    /// <summary>
    /// Hierarchic ViewModel
    /// </summary>
    public class NodeViewModel : AbstractNotifyPropertyChanged
    {
        #region Members, Constructor

        private bool mIsExpanded;
        private bool? mIsChecked;

        /// <summary>
        /// Constructor
        /// </summary>
        internal NodeViewModel( NodeModel model )
        {
            // Tree Stuff
            Children = new List<NodeViewModel>();
            IsExpanded = true;
            IsShow = true;
            mIsChecked = false;

            // Test Stuff
            Model = model ?? throw new ArgumentNullException( nameof( model ) );
            Model.PropertyChanged += OnNodePropertyChanged;
        }

        #endregion

        #region Test Properties

        /// <summary>
        /// Get the model.
        /// </summary>
        public NodeModel Model { get; }

        /// <summary>
        /// Get the text.
        /// </summary>
        public string Text => Model.Text;

        public string TextAddition
        {
            get {
                var result = string.Empty;
                var testCount = Descendents.Count( n => n.Type == TestType.Case );

                if( testCount == 1 ) {
                    result = $"({testCount} test)";
                }
                else if( testCount > 1 ) {
                    result = $"({testCount} tests)";
                }

                return result;
            }
        }

        /// <summary>
        /// Get the tooltip.
        /// </summary>
        public string ToolTip => Model.Path;

        /// <summary>
        /// Get the full name of the node.
        /// </summary>
        public string FullName => Model.FullName;

        /// <summary>
        /// Get the test type.
        /// </summary>
        public TestType Type => Model.Type;

        /// <summary>
        /// Get the test state.
        /// </summary>
        public TestState State
        {
            get => Model.State;
            set => Model.State = value;
        }

        /// <summary>
        /// Get the end time of the test.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Get the start time of the test.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Get the test identifier.
        /// </summary>
        public string Id => Model.Id;

        /// <summary>
        /// Get the class name.
        /// </summary>
        public string ClassName => Model.ClassName;

        /// <summary>
        /// Get the message.
        /// </summary>
        public string Message
        {
            get => Model.Message;
            set => Model.Message = value;
        }

        /// <summary>
        /// Get the message.
        /// </summary>
        public string MessageForTree
        {
            get => State != TestState.Passed ? Message : null;
        }

        /// <summary>
        /// Get the StackTrace.
        /// </summary>
        public string StackTrace
        {
            get => Model.StackTrace;
            set => Model.StackTrace = value;
        }

        /// <summary>
        /// Get true if StackTrace is available
        /// </summary>
        public bool HasStackTrace => !string.IsNullOrEmpty( StackTrace );

        /// <summary>
        /// Get the total time of this node and all sub nodes.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                var result = new TimeSpan();
                DescendantsAndMe.Where(n => n.Type == TestType.Case)
                    .Select( n => n.EndTime - n.StartTime )
                    .Where( t => t > TimeSpan.Zero ).ToList()
                    .ForEach( t => result += t );

                return result;
            }
        }

        #endregion

        #region Tree Stuff - do not change
        #region Tree Properties

        /// <summary>
        /// Get true if this node is checked in the tree.
        /// </summary>
        public bool? IsChecked
        {
            get {
                if( Type == TestType.Case ) {
                    return mIsChecked;
                }
                else {
                    var cases = DescendantsAndMe.Where( n => n.Type == TestType.Case ).ToList();
                    if( cases.All( c => c.IsChecked == true ) ) return true;
                    if( cases.All( c => c.IsChecked == false ) ) return false;
                    return null;
                }
            }
            set {
                if( Type == TestType.Case ) {
                    mIsChecked = value == true;
                }
                else {
                    var cases = DescendantsAndMe.Where( n => n.Type == TestType.Case ).ToList();
                    cases.ForEach( n => n.IsChecked = value );
                }

                OnPropertyChanged();
            }
        }

        public NodeViewModel Parent { get; set; }

        private List<NodeViewModel> Children { get; }

        internal IEnumerable<NodeViewModel> DescendantsAndMe => Descendents.ToList().Append( this );

        internal IEnumerable<NodeViewModel> Descendents
        {
            get {
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
            set {
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
