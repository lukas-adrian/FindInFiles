
using System.Windows;

namespace FindInFiles.ProgressBarWindow;

/// <summary>
/// Waiting Bar
/// </summary>
public partial class ProgressWindow : Window
{

   /// <summary>Cancel the process</summary>
   private CancellationTokenSource _cancellationTokenSource;
   
   /// <summary>
   /// 
   /// </summary>
   /// <param name="cancellationTokenSource"></param>
   public ProgressWindow(CancellationTokenSource cancellationTokenSource)
   {
      InitializeComponent();
      
      _cancellationTokenSource = cancellationTokenSource;
   }
   
   /// <summary>
   /// 
   /// </summary>
   /// <param name="percentage"></param>
   public void UpdateProgress(int percentage)
   {
      progressBar.Value = percentage;
      textBlockProgress.Text = $"Processing... {percentage}%";
      if (percentage >= 100)
      {
         Close();
      }
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   private void butCancel_Click(object sender, RoutedEventArgs e)
   {
      _cancellationTokenSource?.Cancel();
       Close();
   }
}