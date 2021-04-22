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

        internal NodeViewModel( NodeModel model )
        {
            // Tree Stuff
            Children = new List<NodeViewModel>();
            IsExpanded = true;
            IsShow = true;
            mIsChecked = false;

            // Test Stuff
            Model = model ?? throw new System.ArgumentNullException( nameof( model ) );
            Model.PropertyChanged += OnNodePropertyChanged;
        }
        
        #endregion

        #region Test Properties

        public NodeModel Model { get; }

        public string Text => Model.Text;

        public string ToolTip => Model.Path;

        public string Path => Text + "/" + string.Join( "/", Ancestors.Select( a => a.Text ) );

        public string FullName => Model.FullName;

        public TestType Type => Model.Type;

        public TestState State
        {
            get => Model.State;
            set => Model.State = value;
        }

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

        public string Id => Model.Id;

        public string ClassName => Model.ClassName;

        public string MethodName => Model.MethodName;

        public string Message
        {
            get => Model.Message;
            set => Model.Message = value;
        }

        public string StackTrace
        {
            get => Model.StackTrace;
            set => Model.StackTrace = value;
        }

        #endregion


        #region Tree Stuff - do not change
        #region Tree Properties

        public NodeViewModel Parent { get; set; }

        private List<NodeViewModel> Children { get; }

        private IEnumerable<NodeViewModel> Ancestors
        {
            get {
                var result = new List<NodeViewModel>();

                if( Parent != null ) {
                    result.Add( Parent );
                    result.AddRange( Parent.Ancestors );
                }

                return result;
            }
        }

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
