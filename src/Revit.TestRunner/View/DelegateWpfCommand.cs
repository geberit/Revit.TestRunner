using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Revit.TestRunner.View
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }

    public interface IErrorHandler
    {
        void HandleError( Exception ex );
    }


    public class AsyncCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly IErrorHandler _errorHandler;

        public AsyncCommand(
            Func<Task> execute,
            Func<bool> canExecute = null,
            IErrorHandler errorHandler = null )
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public bool CanExecute()
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if( CanExecute() ) {
                try {
                    _isExecuting = true;
                    await _execute();
                }
                finally {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke( this, EventArgs.Empty );
        }

        #region Explicit implementations
        bool ICommand.CanExecute( object parameter )
        {
            return CanExecute();
        }

        void ICommand.Execute( object parameter )
        {
            ExecuteAsync().FireAndForgetSafeAsync( _errorHandler );
        }
        #endregion

    }

    public static class TaskUtilities
    {
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public static async void FireAndForgetSafeAsync( this Task task, IErrorHandler handler = null )
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            try {
                await task;
            }
            catch( Exception ex ) {
                handler?.HandleError( ex );
            }
        }
    }



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
