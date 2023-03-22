using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Revit.TestRunner.Shared;

namespace Revit.TestRunner.App.View.TestTreeView
{
    /// <summary>
    /// ViewModel für die PropertyTree View
    /// </summary>
    public class TreeViewModel : AbstractNotifyPropertyChanged
    {
        #region Members, Constructor

        private NodeViewModel mSelectedNode;
        private string mFilter;

        /// <summary>
        /// Constructor
        /// </summary>
        internal TreeViewModel()
        {
            RootObjects = new List<NodeViewModel>();
            ObjectTree = new ObservableCollection<NodeViewModel>();
            mFilter = string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gibt die Anzuzeigenden Objekte zurück. Es befinden sich nur die sichtbaren Objekte in der Collection.
        /// </summary>
        public ObservableCollection<NodeViewModel> ObjectTree { get; set; }

        /// <summary>
        /// Git die Liste der Root Objekte zurück.
        /// </summary>
        private List<NodeViewModel> RootObjects { get; }

        /// <summary>
        /// Gibt den selektierten Node zurück.
        /// </summary>
        public NodeViewModel SelectedNode
        {
            get => mSelectedNode;
            set
            {
                if( Equals( value, mSelectedNode ) ) return;
                mSelectedNode = value;
                OnPropertyChanged();
            }
        }

        internal bool HasObjects => ObjectTree.Count > 0;

        /// <summary>
        /// Filter String
        /// </summary>
        public string Filter
        {
            get => mFilter;
            set
            {
                if( value == mFilter ) return;
                mFilter = value;
                OnPropertyChanged( () => Filter );

                TreeAsFlatList();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Fügt dem TreeView ein neues Root-Objekt hinzu.
        /// </summary>
        internal void AddRootObject( NodeViewModel root, bool isExpanded )
        {
            root.IsExpanded = isExpanded;
            root.PropertyChanged += OnRootPropertyChanged;
            RootObjects.Add( root );

            TreeAsFlatList();

            OnPropertyChangedAll();
        }

        internal void Clear()
        {
            RootObjects.Clear();
        }

        #endregion

        #region Commands

        public ICommand ClearFilterCommand => new DelegateWpfCommand( () => Filter = string.Empty );

        #endregion

        #region Methods

        /// <summary>
        /// Für alle Root Objekte werden deren Children in eine flache Liste 
        /// </summary>
        private void TreeAsFlatList()
        {
            var list = new List<NodeViewModel>();
            var selected = SelectedNode;
            ObjectTree.Clear();

            // Get all
            foreach( NodeViewModel rootObject in RootObjects ) {
                list.Add( rootObject );
                list.AddRange( rootObject.Descendents.Where( n => n.IsShow ) );
            }

            // Filter
            var filtered = list.Where( n => n.FullName.ToLower().Contains( Filter.ToLower() ) ).ToList();

            // Get filtered
            foreach( NodeViewModel rootObject in RootObjects ) {
                ObjectTree.Add( rootObject );

                foreach( NodeViewModel node in rootObject.Descendents.Where( n => n.IsShow ) ) {
                    if( node.DescendantsAndMe.Any( n => filtered.Contains( n ) ) ) {
                        ObjectTree.Add( node );
                    }
                }
            }

            foreach( var node in ObjectTree ) {
                node.Highlight = Filter;
            }

            SelectedNode = selected;
        }

        /// <summary>
        /// EventHandler für Property Changed der Root Objekte
        /// </summary>
        private void OnRootPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            TreeAsFlatList();
        }

        #endregion
    }
}