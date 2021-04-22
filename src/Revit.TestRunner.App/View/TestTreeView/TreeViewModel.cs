using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

        /// <summary>
        /// Constructor
        /// </summary>
        internal TreeViewModel()
        {
            RootObjects = new List<NodeViewModel>();
            ObjectTree = new ObservableCollection<NodeViewModel>();
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

        #region Methods

        /// <summary>
        /// Für alle Root Objekte werden deren Children in eine flache Liste 
        /// </summary>
        private void TreeAsFlatList()
        {
            var selected = SelectedNode;
            ObjectTree.Clear();

            foreach( NodeViewModel rootObject in RootObjects ) {
                ObjectTree.Add( rootObject );

                foreach( NodeViewModel node in rootObject.Descendents.Where( n => n.IsShow ) ) {
                    ObjectTree.Add( node );
                }
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
