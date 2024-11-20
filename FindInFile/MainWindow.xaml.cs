using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using FindInFiles.Classes;
using FindInFiles.Extensions;
using FindInFiles.ProgressBarWindow;
using FindInFiles.ViewModel;
using ICSharpCode.AvalonEdit.Highlighting;
using PlugInBase;

namespace FindInFiles
{
   public partial class MainWindow : Window
   {
      private readonly GridLength _gLengthOptionsHeight = new(60);

      private ProgressWindow _pgWindow;

      private const UInt32 ONE_MB = 1048576;
      private string _sLastPreviewFile = "";

      /// <summary>
      /// 
      /// </summary>
      public MainWindow()
      {
         InitializeComponent();

         Assembly currentAssembly = Assembly.GetEntryAssembly();
         if (currentAssembly == null)
         {
            currentAssembly = Assembly.GetCallingAssembly();
         }

         object[] companyAttributes = currentAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
         string sCompanyName = ((AssemblyCompanyAttribute)companyAttributes[0]).Company;

         object[] productAttributes = currentAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
         string sProductName = ((AssemblyProductAttribute)productAttributes[0]).Product;

         string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         string sHistoryFilePath = Path.Combine(appDataPath, sCompanyName, sProductName, "searchHistory.json");
         string sSettingsFilePath = Path.Combine(appDataPath, sCompanyName, sProductName, "settings.json");
         vmSearch vm = new(sSettingsFilePath, sHistoryFilePath);

         DataContext = vm;

         Version? ver = currentAssembly.GetName().Version;
         String sVersNo = "unkown Version";
         if (ver != null)
            sVersNo = ver.ToString();

         vm.Title = $"{currentAssembly.GetName().Name} ({sVersNo})";

         vm.LoadSettings();
         vm.LoadHistory();

         SplitterColumn.Width = new GridLength(0);
         PreviewColumn.Width = new GridLength(0);

         vm.ProgressChanged += Vm_ProgressChanged;
         vm.ProgressCompleted += Vm_ProgressCompleted;
      }

      #region private Functions


      /// <summary>
      /// 
      /// </summary>
      private void ExpandTree()
      {
         // Disable processing of the Dispatcher queue to improve performance
         using (Dispatcher.DisableProcessing())
         {
            ExpandTreeItems(tvResult.Items);
         }

         void ExpandTreeItems(ItemCollection items)
         {
            foreach (var item in items)
            {
               var treeViewItem = tvResult.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
               if (treeViewItem != null)
               {
                  treeViewItem.IsExpanded = true; // Expand current item

                  // Process child items
                  ExpandTreeItems(treeViewItem.Items);
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      private void ShowCurrentFileInPrewView(SearchResult searchResult, int LineNumberSelectIn = 0)
      {
         if (PreviewColumn.Width.Value == 0)
            return;

         if (searchResult == null) return;

         vmSearch vm = DataContext as vmSearch;

         string sFilePath = searchResult.FilePath;

         if (string.Compare(_sLastPreviewFile, sFilePath, StringComparison.OrdinalIgnoreCase) != 0)
         {
            _sLastPreviewFile = sFilePath;

            tbPreview.IsReadOnly = true;
            tbPreview.ShowLineNumbers = true;

            if (vm.dicLineNumbers.TryGetValue(sFilePath, out UInt64 outByteLength))
            {
               if (outByteLength > (ONE_MB * vm.MaxPreviewFileSizeMB))
               {
                  StringBuilder sbText = new StringBuilder();
                  sbText.AppendLine($"File {Path.GetFileName(sFilePath)} is bigger than {vm.MaxPreviewFileSizeMB} MB");
                  sbText.AppendLine($"Current File size is {((double)outByteLength / (double)ONE_MB).ToString("0.00", CultureInfo.CurrentUICulture)} MB");

                  tbPreview.Text = sbText.ToString();
                  return;
               }
            }

            tbPreview.Load(sFilePath);

         }

         if (LineNumberSelectIn != 0)
         {
            tbPreview.ScrollToLine(LineNumberSelectIn);
            tbPreview.Select(LineNumberSelectIn, 15);

            //Select the line
            int offset = tbPreview.Document.GetOffset(LineNumberSelectIn, 0); // Get the offset of the start of the line
            var line = tbPreview.Document.GetLineByNumber(LineNumberSelectIn); // Get the length of the line
            int lineLength = 0;
            if (line != null)
            {
               lineLength = line.Length;
            }

            tbPreview.SelectionStart = offset;
            tbPreview.SelectionLength = lineLength;
         }


         HighlightingManager? hlManager = HighlightingManager.Instance;
         string extension = System.IO.Path.GetExtension(sFilePath);
         IHighlightingDefinition HighlightingDefinition = hlManager.GetDefinitionByExtension(extension);
         tbPreview.SyntaxHighlighting = HighlightingDefinition;
      }


      #endregion private Functions

      #region Events

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PreViewSplitter_OnDragCompleted(Object sender, DragCompletedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;

         vm.MaxPreviewWidth = PreviewColumn.Width.Value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void CbSearchPath_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.AddItemToPath(cbSearchPath.SelectedItem?.ToString());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void CbSearchText_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.AddItemToText(cbSearchText.SelectedItem?.ToString());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void CbSearchExt_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.AddItemToExt(cbSearchExt.SelectedItem?.ToString());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ButPreview_Click(Object sender, RoutedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;

         if (PreviewColumn.Width.Value == 0)
         {
            SplitterColumn.Width = new GridLength(4);
            PreviewColumn.Width = new GridLength(vm.MaxPreviewWidth);
            butPreview.Content = "<<<";

            Tuple<SearchResult, FoundItem?>? foundItems = Helper.GetCurrentTreeViewItem(tvResult, tvResult.SelectedItem);
            if (foundItems is { Item2: not null })
               ShowCurrentFileInPrewView(foundItems.Item1, foundItems.Item2.LineNumber);
         }
         else
         {
            PreviewColumn.Width = new GridLength(0); // Hide panel
            SplitterColumn.Width = new GridLength(0);
            butPreview.Content = ">>>";
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ButOptions_Click(Object sender, RoutedEventArgs e)
      {
         if (OptionsRow.Height.Value == 0)
         {
            OptionsRow.Height = _gLengthOptionsHeight;
            TextSearchRow.Height = new GridLength(TextSearchRow.Height.Value + OptionsRow.Height.Value);
         }
         else
         {
            OptionsRow.Height = new GridLength(0);
            TextSearchRow.Height = new GridLength(TextSearchRow.Height.Value - _gLengthOptionsHeight.Value);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TvResult_OnSelectedItemChanged(Object sender, RoutedPropertyChangedEventArgs<Object> e)
      {
         Tuple<SearchResult, FoundItem?>? foundItems = Helper.GetCurrentTreeViewItem(sender as TreeView, e.NewValue);
         if (foundItems is { Item2: not null })
            ShowCurrentFileInPrewView(foundItems.Item1, foundItems.Item2.LineNumber);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ButPath_Click(object sender, RoutedEventArgs e)
      {
         var openFolderDialog = new Microsoft.Win32.OpenFolderDialog
         {
            Title = "Select a Directory",
            DefaultDirectory = @"C:\"
         };

         if (openFolderDialog.ShowDialog() == true)
         {
            string selectedPath = openFolderDialog.FolderName;

            vmSearch vm = DataContext as vmSearch;

            if (vm.SearchPaths.All(path => string.Compare(path, selectedPath, StringComparison.OrdinalIgnoreCase) != 0))
               vm.SearchPaths.Add(selectedPath);

            cbSearchPath.SelectedItem = selectedPath; // Set selected path
            vm.AddItemToPath(cbSearchPath.Text);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="percent"></param>
      private void Vm_ProgressChanged(object sender, int percent)
      {
         _pgWindow.UpdateProgress(percent); // Update progress bar in ProgressWindow
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Vm_ProgressCompleted(object sender, EventArgs e)
      {
         _pgWindow.Close(); // Close ProgressWindow on completion
         gSearch.IsEnabled = true;

         vmSearch vm = DataContext as vmSearch;

         if (vm.FilesCountAll == 1)
            tbStatusBar.Text = $"in {vm.SearchResultList.Count} of {vm.FilesCountAll} file found";
         else
            tbStatusBar.Text = $"in {vm.SearchResultList.Count} of {vm.FilesCountAll} files found";

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ButSearch_Click(object sender, RoutedEventArgs e)
      {

         vmSearch vm = DataContext as vmSearch;

         //vm.LoadData(); // Load data into SearchResultList
         //vm.GroupResults(); // Group the results after loading
         string folderPath = cbSearchPath.Text; // Get folder path from ComboBox
         if (!Directory.Exists(folderPath)) return;

         string searchTerm = cbSearchText.Text; // Get search term from ComboBox
         if (string.IsNullOrEmpty(searchTerm)) return;

         //string fileExtensions = cbSearchExt.Text; // Get file extension from ComboBox

         //bool bSubDirs = chbSubDirs.IsChecked!.Value;

         vm.CancellationTokenSource = new CancellationTokenSource();
         _pgWindow = new ProgressWindow(vm.CancellationTokenSource);
         _pgWindow.Owner = this;
         _pgWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
         _pgWindow.Show();


         (DataContext as vmSearch)?.SearchCommand.Execute(null);

         gSearch.IsEnabled = false;

         vm.AddItemsSearchHistory(cbSearchText.Text, cbSearchPath.Text, cbSearchExt.Text);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OpenFile_Click(object sender, RoutedEventArgs e)
      {
         Tuple<SearchResult, FoundItem?>? foundItems = Helper.GetCurrentTreeViewItem(tvResult, tvResult.SelectedItem);

         if (foundItems != null && File.Exists(foundItems?.Item1.FilePath))
         {
            try
            {
               System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(foundItems?.Item1.FilePath) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
               if (ex.NativeErrorCode == 193)
               {
                  MessageBox.Show("The file is not a valid executable. The system will try to open it with the associated application.");
                  try
                  {
                     System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select," + foundItems?.Item1.FilePath) { UseShellExecute = true });
                  }
                  catch (Exception innerEx)
                  {
                     MessageBox.Show($"Error opening file {foundItems?.Item1.FilePath}: {innerEx.Message}");
                  }
               }
               else
               {
                  MessageBox.Show($"Error opening file {foundItems?.Item1.FilePath}: {ex.Message}");
               }
            }
         }
         else
         {
            MessageBox.Show("File not found.");
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
      {
         Tuple<SearchResult, FoundItem?>? foundItems = Helper.GetCurrentTreeViewItem(tvResult, tvResult.SelectedItem);

         if (File.Exists(foundItems?.Item1.FilePath))
         {
            string arguments = $"/select, \"{foundItems?.Item1.FilePath}\"";
            Process.Start("explorer.exe", arguments);
         }
         else
         {
            MessageBox.Show("File not found.");
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ClearSearchResults_Click(Object sender, RoutedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.SearchResultList.Clear();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ButDelHistory_Click(Object sender, RoutedEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.DeleteHistory();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void MainWindow_OnClosing(Object? sender, CancelEventArgs e)
      {
         vmSearch vm = DataContext as vmSearch;
         vm.SaveHistory();
         vm.SaveSettings();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void RemoveButton_Click(object sender, RoutedEventArgs e)
      {
         var button = sender as Button;

         if (button?.CommandParameter is Object[] objParameters)
         {
            vmSearch vm = DataContext as vmSearch;

            if (objParameters[0] == cbSearchText)
               vm.RemoveItemText(objParameters[1].ToString());
            else if (objParameters[0] == cbSearchPath)
               vm.RemoveItemPath(objParameters[1].ToString());
            else if (objParameters[0] == cbSearchExt)
               vm.RemoveItemExt(objParameters[1].ToString());
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void CtrlExpandTree_Click(Object sender, RoutedEventArgs e)
      {
         ExpandTree();
      }

      private void TbMinMax_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         if (!char.IsDigit(e.Text, 0) && e.Text != ".")
         {
            e.Handled = true;
         }

         if ((e.Text == ".") && ((sender as TextBox).Text.IndexOf('.') > -1))
         {
            e.Handled = true;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TbNumberic_Pasting(object sender, DataObjectPastingEventArgs e)
      {
         string clipboardText = e.DataObject.GetData(typeof(string)) as string;
         if (clipboardText != null && !clipboardText.IsNumeric())
         {
            e.CancelCommand(); // Prevent the paste operation
         }
      }

      #endregion Events

   }

}