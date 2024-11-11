
using System.Windows;

namespace FindInFile.ProgressBarWindow;

public partial class ProgressWindow : Window
{
   private bool _isCancelled = false;
   
   public ProgressWindow()
   {
      InitializeComponent();
   }
   
   public void UpdateProgress(int percentage)
   {
      progressBar.Value = percentage;
      textBlockProgress.Text = $"Processing... {percentage}%";
      if (percentage == 100)
      {
         Close();
      }
   }

   public bool IsCancelled
   {
      get { return _isCancelled; }
   }

   private void butCancel_Click(object sender, RoutedEventArgs e)
   {
      _isCancelled = true;
      Close();
   }
}