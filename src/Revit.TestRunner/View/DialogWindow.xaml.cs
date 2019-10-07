using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;

namespace Revit.TestRunner.View
{
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow
    {
        public DialogWindow()
        {
            UseLayoutRounding = true;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            InitializeComponent();
        }

        /// <summary>
        /// Verbindet ein ViewModel mit einem View und zeigt dieses in einem
        /// modalen Dialog-Fenster.
        /// </summary>
        /// <remarks>
        /// Wenn im View eine Breite via MinWidth-Feld angegeben wurde, wird das re-sizing 
        /// eingeschaltet.
        /// </remarks>
        public static bool? Show<TView>( DialogViewModel viewModel )
            where TView : FrameworkElement, new()
        {
            if( viewModel == null ) throw new ArgumentNullException( nameof(viewModel) );

            // create new view and a window to hold view
            var view = new TView();
            var window = new DialogWindow();
            window.Content = view;
            window.DataContext = viewModel;

            // is MaxWidth/Height is specified, apply it to Window
            if( !double.IsPositiveInfinity( view.MaxHeight ) )
                window.MaxHeight = CalculateWindowHeight( view.MaxHeight );
            if( !double.IsPositiveInfinity( view.MaxWidth ) )
                window.MaxWidth = CalculateWindowWidth( view.MaxWidth );

            // if MinWidth is specified, switch resizing on
            if( view.MinWidth > 0.0 ) {
                window.ResizeMode = ResizeMode.CanResizeWithGrip;
                window.MinWidth = CalculateWindowWidth( view.MinWidth );
                window.MinHeight = CalculateWindowHeight( view.MinHeight );
                if( !double.IsNaN( view.Width ) && !double.IsNaN( view.Height ) ) {
                    // if width specified use it as initial size
                    window.Width = CalculateWindowWidth( view.Width );
                    window.Height = CalculateWindowHeight( view.Height );
                    view.Width = double.NaN;
                    view.Height = double.NaN;
                    window.SizeToContent = SizeToContent.Manual;
                }
                else if( !double.IsNaN( viewModel.InitialWidth ) && !double.IsNaN( viewModel.InitialHeight ) ) {
                    // if width specified use it as initial size
                    window.Width = viewModel.InitialWidth;
                    window.Height = viewModel.InitialHeight;
                    window.SizeToContent = SizeToContent.Manual;
                }
            }

            // Parent Window setzen
            System.Windows.Interop.WindowInteropHelper windowInteropHelper = new System.Windows.Interop.WindowInteropHelper( window );
            windowInteropHelper.Owner = Process.GetCurrentProcess().MainWindowHandle;

            // set AutomationId
            AutomationProperties.SetAutomationId( window, viewModel.GetType().Name );

            try {
                // set Handler to close dialog after OK
                viewModel.SetDialogResultAction = result => window.DialogResult = result;
                return window.ShowDialog();
            }
            finally {
                viewModel.SetDialogResultAction = null;
                window.DataContext = null;
            }
        }

        /// <summary>
        /// Gibt die Breite des Fensters zurück für die angegebene Clientgrösse.
        /// </summary>
        private static double CalculateWindowWidth( double viewWidth )
        {
            return viewWidth + 4 * SystemParameters.ResizeFrameVerticalBorderWidth;
        }

        /// <summary>
        /// Gibt die Breite des Fensters zurück für die angegebene Clientgrösse.
        /// </summary>
        private static double CalculateWindowHeight( double viewHeight )
        {
            return viewHeight + SystemParameters.WindowCaptionHeight + 4 * SystemParameters.ResizeFrameHorizontalBorderHeight;
        }
    }
}
