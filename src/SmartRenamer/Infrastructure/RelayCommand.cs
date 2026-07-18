using System;
using System.Windows.Input;

namespace SmartRenamer.Infrastructure
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;

        public RelayCommand(Action execute)
            : this(_ => execute(), null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(
                _ => execute(),
                _ => canExecute())
        {
        }

        public RelayCommand(
            Action<object?> execute,
            Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}