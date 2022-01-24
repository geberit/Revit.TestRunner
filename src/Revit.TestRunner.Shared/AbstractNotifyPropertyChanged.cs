using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Revit.TestRunner.Shared
{
    /// <summary>
    /// BaseClass for WPF ViewModels
    /// </summary>
    public abstract class AbstractNotifyPropertyChanged : INotifyPropertyChanged
    {
        public virtual event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged( PropertyChangedEventArgs e )
        {
            PropertyChanged?.Invoke( this, e );
        }

        protected void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            OnPropertyChanged( new PropertyChangedEventArgs( propertyName ) );
        }

        protected void OnPropertyChanged<T>( Expression<Func<T>> action )
        {
            var propertyName = GetPropertyName( action );

            OnPropertyChanged( new PropertyChangedEventArgs( propertyName ) );
        }

        protected void OnPropertyChangedAll()
        {
            OnPropertyChanged( new PropertyChangedEventArgs( string.Empty ) );
        }

        public string GetPropertyName<T>( Expression<Func<T>> expression )
        {
            return GetName( expression );
        }

        public static string GetName<T>( Expression<Func<T>> aExpression )
        {
            var memberExpression = (MemberExpression)aExpression.Body;

            return memberExpression.Member.Name;
        }
    }
}
