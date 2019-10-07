using System;
using System.Windows.Input;

namespace Revit.TestRunner.View
{
    /// <summary>
    /// Ein Kommando welches die Ausfuehrung, sowie das Ueberpruefen der Ausfuehrbarkeit an zwei Delegaten weiterleitet.
    /// </summary>
    public sealed class DelegateWpfCommand : ICommand
    {
        private readonly Action mAction;

        private readonly Func<bool> mCanExecute;

        public DelegateWpfCommand( Action action, Func<bool> canExecute = null )
        {
            mCanExecute = canExecute;
            mAction = action ?? throw new ArgumentNullException( nameof( action ) );
        }

        public void Execute( object parameter )
        {
            mAction();
        }

        public bool CanExecute( object parameter )
        {
            return mCanExecute == null || mCanExecute();
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }
    }
}
