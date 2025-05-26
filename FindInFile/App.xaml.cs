using System.Windows;
using System.Windows;
using Serilog;

namespace FindInFiles;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
   public App()
   {
      // Set up the global Serilog logger
      Log.Logger = new LoggerConfiguration()
         .MinimumLevel.Debug() // Set minimum log level here
         .WriteTo.Console()
         .CreateLogger();

      // You can now use Serilog.Log or Serilog.Log.ForContext<T>() anywhere in your app
   }
}