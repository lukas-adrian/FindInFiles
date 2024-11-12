using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FindInFile.Classes;
using FindInFile.ProgressBarWindow;
using FindInFile.ViewModel;
using ICSharpCode.AvalonEdit.Highlighting;
using Newtonsoft.Json;

namespace FindInFile
{
    public partial class MainWindow : Window
    {
        private GridLength _gLengthPreviewWidth = new(400);
        private readonly GridLength _gLengthOptionsHeight = new(20);

        private SearchHistory _SearchHistory;
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
                    if (outByteLength > (ONE_MB * vm.MaxPreviewSize))
                    {
                        StringBuilder sbText = new StringBuilder();
                        sbText.AppendLine($"File {Path.GetFileName(sFilePath)} is bigger than {vm.MaxPreviewSize} MB");
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



        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="results"></param>
        // private void UpdateUIWithResults(List<SearchResult> results)
        // {
        //     dgResult.ItemsSource = results; // Bind results to DataGrid
        // }




        #endregion private Functions
        
        #region Events
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreViewSplitter_OnDragCompleted(Object sender, DragCompletedEventArgs e)
        {
            _gLengthPreviewWidth = PreviewColumn.Width;
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
            if (PreviewColumn.Width.Value == 0)
            {
                SplitterColumn.Width = new GridLength(4);
                PreviewColumn.Width = _gLengthPreviewWidth;
                butPreview.Content = "<<<";
                 
                 if (tvResult.SelectedItem is FoundItem foundItem)
                 {
                     // Find the parent SearchResult
                     foreach (var searchResult in tvResult.ItemsSource as ObservableCollection<SearchResult>)
                     {
                         if (searchResult.FoundItems.Contains(foundItem))
                         {
                             ShowCurrentFileInPrewView(searchResult, foundItem.LineNumber);
                             break;
                         }
                     }
                 }
            }
            else
            {
                PreviewColumn.Width = new GridLength(0); // Hide panel
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
                TextSearchRow.Height = new GridLength(TextSearchRow.Height.Value + _gLengthOptionsHeight.Value);
            }
            else
            {
                OptionsRow.Height = new GridLength(0);
                TextSearchRow.Height = new GridLength(TextSearchRow.Height.Value - _gLengthOptionsHeight.Value);
            }
        }
        
        
        private void TvResult_OnSelectedItemChanged(Object sender, RoutedPropertyChangedEventArgs<Object> e)
        {
            FoundItem selectedItem = e.NewValue as FoundItem;

            if (selectedItem is FoundItem foundItem)
            {
                // Find the parent SearchResult
                foreach (var searchResult in (sender as TreeView).ItemsSource as ObservableCollection<SearchResult>)
                {
                    if (searchResult.FoundItems.Contains(foundItem))
                    {
                        ShowCurrentFileInPrewView(searchResult, selectedItem.LineNumber);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridResults_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            // if (dgResult.SelectedItem == null)
            //     return;
            //
            // SearchResult? sr = dgResult.SelectedItem as SearchResult;
            //
            // if (sr != null)
            // {
            //     foreach (var cellInfo in e.AddedCells)
            //     {
            //         var cell = cellInfo.Column.GetCellContent(cellInfo.Item) as FrameworkElement;
            //         if (cell != null)
            //         {
            //             var listBox = FindVisualChild<ListBox>(cell);
            //             if (listBox != null && listBox.SelectedItem is int selectedLineNumber)
            //             {
            //                 ShowCurrentFileInPrewView(sr, selectedLineNumber);
            //             }
            //         }
            //     }
            // }
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgSearchResultLineNumber_OnSelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            // var listBox = sender as ListBox;
            // if (listBox?.SelectedItem == null) return;
            //
            // DataGridRow dataGridRow = FindVisualParentDataGridRow(listBox);
            // dgResult.SelectedItem = dataGridRow.DataContext;
            //
            // SearchResult? sr = dgResult.SelectedItem as SearchResult;
            // if (string.Compare(_sLastPreviewFile, sr.FilePath) == 0)
            // {
            //     int selectedLineNumber = (int)listBox.SelectedItem;
            //     ShowCurrentFileInPrewView(sr, selectedLineNumber);
            // }
        }

        
        private DataGridRow FindVisualParentDataGridRow(DependencyObject child)
        {
            while (child != null)
            {
                if (child is DataGridRow row)
                {
                    return row;
                }
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        // Helper method to find a child element of a given type
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T correctlyTyped)
                {
                    return correctlyTyped;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
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

                if(vm.SearchPaths.All(path => string.Compare(path, selectedPath, StringComparison.OrdinalIgnoreCase) != 0))
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
            butSearch.IsEnabled = true;

            vmSearch vm = DataContext as vmSearch;
            tbStatusBar.Text = $"{vm.SearchResultList.Count} found";

        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButSearch_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = cbSearchPath.Text; // Get folder path from ComboBox
            if (!Directory.Exists(folderPath)) return;
            
            string searchTerm = cbSearchText.Text; // Get search term from ComboBox
            if (string.IsNullOrEmpty(searchTerm)) return;
            
            string fileExtensions = cbSearchExt.Text; // Get file extension from ComboBox

            bool bSubDirs = chbSubDirs.IsChecked!.Value;
            
            vmSearch vm = DataContext as vmSearch;
            
            vm.CancellationTokenSource = new CancellationTokenSource();
            _pgWindow = new ProgressWindow(vm.CancellationTokenSource);
            _pgWindow.Owner = this;
            _pgWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _pgWindow.Show();
            
            // Execute command instead of BackgroundWorker
            var args = new SearchArgs 
            { 
                Path = folderPath, 
                Extensions = fileExtensions, 
                SearchTerm = searchTerm, 
                SubDirs = bSubDirs 
            };
        
            (DataContext as vmSearch)?.SearchCommand.Execute(args);
        
            butSearch.IsEnabled = false;
        
            vm.AddItemsSearchHistory(cbSearchText.Text, cbSearchPath.Text, cbSearchExt.Text);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // SearchResult? sr = dgResult.SelectedItem as SearchResult;
            // if (sr == null)
            //     return;
            //
            // if (File.Exists(sr.FilePath))
            // {
            //     Process.Start(sr.FilePath);
            // }
            // else
            // {
            //     MessageBox.Show("File not found.");
            // }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            // SearchResult? sr = dgResult.SelectedItem as SearchResult;
            // if (sr == null)
            //     return;
            //
            // if (File.Exists(sr.FilePath))
            // {
            //     string arguments = $"/select, \"{sr.FilePath}\"";
            //     Process.Start("explorer.exe", arguments);
            // }
            // else
            // {
            //     MessageBox.Show("File not found.");
            // }
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
                
                if(objParameters[0] == cbSearchText)
                    vm.RemoveItemText(objParameters[1].ToString());
                else if(objParameters[0] == cbSearchPath)
                    vm.RemoveItemPath(objParameters[1].ToString());
                else if(objParameters[0] == cbSearchExt)
                    vm.RemoveItemExt(objParameters[1].ToString());
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButExpandTree_Click(Object sender, RoutedEventArgs e)
        {
            ExpandTree();
        } 
        
        #endregion Events
    }

}