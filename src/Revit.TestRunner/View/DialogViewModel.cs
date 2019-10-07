using System;
using System.Windows.Input;

namespace Revit.TestRunner.View
{
    /// <summary>
	/// Basis Klasse für ViewModels die in einem UserControl verwendet werden, das
	/// mit DialogWindow als modaler Dialog angezeigt werden soll.
	/// </summary>
	public abstract class DialogViewModel : AbstractViewModel
    {
        private string mDisplayName;
        internal Action<bool> SetDialogResultAction;

        protected DialogViewModel()
        {
            mDisplayName = "?";
        }

        /// <summary>
        /// Kann verwendet werden um als Titel in einem Dialog-Fenster anzuzeigen.
        /// </summary>
        public string DisplayName
        {
            get => mDisplayName;
            set
            {
                mDisplayName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Wird verwendet um die Anfangs-Grösse des Dialogs zu beeinflussen.
        /// </summary>
        public double InitialWidth { get; protected set; } = double.NaN;

        /// <summary>
        /// Wird verwendet um die Anfangs-Grösse des Dialogs zu beeinflussen.
        /// </summary>
        public double InitialHeight { get; protected set; } = double.NaN;

        /// <summary>
        /// Bindable-Command für einen OK-Button. Er verursacht, dass wenn ExecuteButtonOk
        /// true zurück gibt, das modale Dialog-Fenster geschlossen wird.
        /// </summary>
        public ICommand ButtonOkCommand
        {
            get
            {
                return new DelegateWpfCommand( () => {
                    if( ExecuteButtonOk() ) SetDialogResult();
                },
                    CanExecuteButtonOk );
            }
        }

        /// <summary>
        /// Bestimmt, ob der OK-Button Enabled ist oder nicht.
        /// </summary>
        protected virtual bool CanExecuteButtonOk() { return true; }

        /// <summary>
        /// Führt den Befehl für den OK-Button aus.
        /// Wenn der Dialog nach OK geschlossen werden soll muss True
        /// zurück gegeben werden.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExecuteButtonOk() { return true; }

        private void SetDialogResult( bool dialogResult = true )
        {
            SetDialogResultAction?.Invoke( dialogResult );
        }
    }
}
