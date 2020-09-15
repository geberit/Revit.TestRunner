using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Revit.TestRunner.App.View
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
        private bool mIsExecuting;
        private readonly Func<Task> mExecute;
        private readonly Func<bool> mCanExecute;
        private readonly IErrorHandler mErrorHandler;

        public AsyncCommand( Func<Task> execute, Func<bool> canExecute = null, IErrorHandler errorHandler = null )
        {
            mExecute = execute;
            mCanExecute = canExecute;
            mErrorHandler = errorHandler;
        }

        public bool CanExecute()
        {
            return !mIsExecuting && (mCanExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if( CanExecute() ) {
                try {
                    mIsExecuting = true;
                    await mExecute();
                }
                finally {
                    mIsExecuting = false;
                }
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        #region Explicit implementations
        bool ICommand.CanExecute( object parameter )
        {
            return CanExecute();
        }

        void ICommand.Execute( object parameter )
        {
            ExecuteAsync().FireAndForgetSafeAsync( mErrorHandler );
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
            add {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove {
                if( mCanExecute != null ) {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }
    }
}
