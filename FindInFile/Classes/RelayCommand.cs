using System.Windows.Input;

namespace FindInFile.Classes;

/// <summary>
/// 
/// </summary>
public class RelayCommand : ICommand
{
   private readonly Action<object> _execute;
   private readonly Predicate<object> _canExecute;

   /// <summary>
   /// 
   /// </summary>
   /// <param name="execute"></param>
   /// <param name="canExecute"></param>
   /// <exception cref="ArgumentNullException"></exception>
   public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
   {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
   }

   /// <summary>
   /// 
   /// </summary>
   public event EventHandler CanExecuteChanged
   {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="parameter"></param>
   /// <returns></returns>
   public bool CanExecute(object parameter)
   {
      return _canExecute == null || _canExecute(parameter);
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="parameter"></param>
   public void Execute(object parameter)
   {
      _execute(parameter);
   }
}