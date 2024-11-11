using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using FindInFile.ProgressBarWindow;
using FindInFile.ViewModel;
using ICSharpCode.AvalonEdit.Highlighting;
using Newtonsoft.Json;

namespace FindInFile
{
    public partial class MainWindow : Window
    {
            
        /// <summary>
        /// 
        /// </summary>
        private class SearchArgs
        {
            public string Path { get; set; }
            public string Extensions { get; set; }
            public string SearchTerm { get; set; }
            public bool SubDirs { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private class SearchResult
        {
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
        }

        private readonly string HistoryFilePath = "searchHistory.json";
        private readonly string SettingsFilePath = "settings.json";

        private GridLength _gLengthPreviewWidth = new(400);
        private readonly GridLength _gLengthOptionsHeight = new(20);

        private SearchHistory _SearchHistory;
        
        private BackgroundWorker _bwSearch;
        private ProgressWindow _pgWindow;

        private const UInt32 ONE_MB = 1048576;
        private static Dictionary<string, UInt64> dicLineNumbers = new();
        private string _sLastPreviewFile = "";

        /// <summary>
        /// 
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            var viewModel = new vmSearch();
            DataContext = viewModel;
            
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
            HistoryFilePath = Path.Combine(appDataPath, sCompanyName, sProductName, HistoryFilePath);
            
            SettingsFilePath = Path.Combine(appDataPath, sCompanyName, sProductName, SettingsFilePath);
            Version? ver = currentAssembly.GetName().Version;
            String sVersNo = "unkown Version";
            if (ver != null)
               sVersNo = ver.ToString();

            viewModel.Title = $"{currentAssembly.GetName().Name} ({sVersNo})";

            LoadSettings();
            LoadHistory(); 
            
            SplitterColumn.Width = new GridLength(0);
            PreviewColumn.Width = new GridLength(0);
            
            _bwSearch = new BackgroundWorker();
            _bwSearch.DoWork += bwSearchDoWork;
            _bwSearch.ProgressChanged += bwSearchProgressChanged;
            _bwSearch.RunWorkerCompleted += bwSearchRunWorkerCompleted;
            _bwSearch.WorkerReportsProgress = true;
        }


        #region Backgroundworker
        
        private void bwSearchDoWork(object sender, DoWorkEventArgs e)
        {
            SearchArgs? args = (SearchArgs)e.Argument!;
            var foundResults = SearchInFolder(args.Path, args.Extensions, args.SearchTerm, args.SubDirs, (sender as BackgroundWorker));
            e.Result = foundResults;
            if (_pgWindow.IsCancelled)
            {
                e.Cancel = true;
            }
        }

        private void bwSearchProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _pgWindow.UpdateProgress(e.ProgressPercentage);
        }

        private void bwSearchRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Error searching in folder: {e.Error.Message}");
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("Search cancelled.");
            }
            else
            {
                // Update UI with search results
                UpdateUIWithResults((List<SearchResult>)e.Result);
            }
            butSearch.IsEnabled = true; // Enable the button again
            _pgWindow.Close();
        }
        
        #endregion Backgroundworker
        
        #region private Functions
        
        /// <summary>
        /// 
        /// </summary>
        private void ShowCurrentFileInPrewView()
        {
            if (_gLengthPreviewWidth.Value == 0)
                return;
            
            if (dgResult.SelectedItem == null)
                return;
            

            SearchResult? sr = dgResult.SelectedItem as SearchResult;
            if (sr == null) return;
            
            string sFilePath = sr.FilePath;
            int nLineNumber = sr.LineNumber;

            if (string.Compare(_sLastPreviewFile, sFilePath, StringComparison.OrdinalIgnoreCase) == 0)
                return;

            _sLastPreviewFile = sFilePath;
            vmSearch vm = DataContext as vmSearch;
            
            if (dicLineNumbers.TryGetValue(sFilePath, out UInt64 outByteLength))
            {
                if (outByteLength > (ONE_MB * vm.MaxPreviewSize))
                {
                    StringBuilder sbText = new StringBuilder();
                    sbText.AppendLine($"File {Path.GetFileName(sFilePath)} is bigger than {vm.MaxPreviewSize} MB");
                    sbText.AppendLine($"Current File size is {((double)outByteLength / (double)ONE_MB).ToString("0.00")} MB");
                    
                    tbPreview.Text = sbText.ToString();
                    return;
                }
            }


            tbPreview.IsReadOnly = true;
            tbPreview.ShowLineNumbers = true;
            tbPreview.Load(sFilePath);
            tbPreview.ScrollToLine(nLineNumber);
            tbPreview.Select(nLineNumber,15);
            
            int offset = tbPreview.Document.GetOffset(nLineNumber, 0); // Get the offset of the start of the line
            var line = tbPreview.Document.GetLineByNumber(nLineNumber); // Get the length of the line
            int lineLength = 0;
            if (line != null)
            {
                lineLength = line.Length;
                // Use lineLength as needed
            }
            
            tbPreview.SelectionStart = offset;
            tbPreview.SelectionLength = lineLength;
            
            HighlightingManager? hlManager = HighlightingManager.Instance;
            string extension = System.IO.Path.GetExtension(sFilePath);
            IHighlightingDefinition HighlightingDefinition = hlManager.GetDefinitionByExtension(extension);
            tbPreview.SyntaxHighlighting = HighlightingDefinition;
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void LoadHistory()
        {
            // Clear existing items before loading new history
            vmSearch vm = DataContext as vmSearch;
            vm.SearchPaths.Clear();
            vm.SearchExtensions.Clear();
            vm.SearchTexts.Clear();

            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                _SearchHistory = JsonConvert.DeserializeObject<SearchHistory>(json);

                if (_SearchHistory != null)
                {

                    foreach (string sPath in _SearchHistory.Path)
                    {
                        vm.SearchPaths.Add(sPath);
                    }
                    foreach (string sPath in _SearchHistory.Extension)
                    {
                        vm.SearchExtensions.Add(sPath);
                    }
                    foreach (string sPath in _SearchHistory.Text)
                    {
                        vm.SearchTexts.Add(sPath);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SaveHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                _SearchHistory = JsonConvert.DeserializeObject<SearchHistory>(json) ?? new SearchHistory();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFilePath));
                _SearchHistory = new SearchHistory();
            }

            _SearchHistory.Path.Add(cbSearchPath.Text);
            _SearchHistory.Extension.Add(cbSearchExt.Text);
            _SearchHistory.Text.Add(cbSearchText.Text);
            
            var updatedJson = JsonConvert.SerializeObject(_SearchHistory, Formatting.Indented);
            File.WriteAllText(HistoryFilePath, updatedJson); // Save to file
            
            
            vmSearch vm = DataContext as vmSearch;
                
            vm.SearchPaths = new ObservableCollection<String>(_SearchHistory.Path);
            vm.SearchExtensions = new ObservableCollection<String>(_SearchHistory.Extension);
            vm.SearchTexts = new ObservableCollection<String>(_SearchHistory.Text);
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadSettings()
        {
            vmSearch vm = DataContext as vmSearch;
            if (!File.Exists(SettingsFilePath))
            {//Default value
                vm.MaxPreviewSize = 5;
            }
            else
            {
                string json = File.ReadAllText(SettingsFilePath);
                var jsonObject = JsonConvert.DeserializeAnonymousType(json, new { MaxPreviewSize = 0 });
                vm.MaxPreviewSize = Convert.ToUInt32(jsonObject.MaxPreviewSize);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extensions"></param>
        /// <param name="searchTerm"></param>
        /// <param name="subDirs"></param>
        /// <returns></returns>
        private List<SearchResult> SearchInFolder(string path, string extensions, string searchTerm, bool subDirs, BackgroundWorker? worker)
        {
            dicLineNumbers = new Dictionary<String, UInt64>();
            var foundResults = new List<SearchResult>();
            var extensionList = extensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim()).ToList(); // Trim whitespace

            try
            {
                int totalFiles = 0;
                int processedFiles = 0;
                
                foreach (var extension in extensionList)
                {
                    IEnumerable<String> files = null;

                    if (subDirs)
                        files = Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.AllDirectories);
                    else
                        files = Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.TopDirectoryOnly);

                    totalFiles += files.Count();
                        
                    foreach (var file in files)
                    {
                        FileContainsTermUsingBytes(file, searchTerm, foundResults);
                        processedFiles++;
                        worker.ReportProgress((int)((processedFiles / (float)totalFiles) * 100));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching in folder: {ex.Message}");
            }

            return foundResults;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="results"></param>
        private void UpdateUIWithResults(List<SearchResult> results)
        {
            dgResult.ItemsSource = results; // Bind results to DataGrid
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="searchTerm"></param>
        /// <param name="foundResults"></param>
        private void FileContainsTermUsingBytes(string filePath, string searchTerm, List<SearchResult> foundResults)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath); // Read all bytes from the file
                string content = Encoding.UTF8.GetString(fileBytes); // Convert bytes to string
                
                int lineNumber = 0;
                using (StringReader reader = new(content))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        if (line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) // Case-insensitive search
                        {
                            foundResults.Add(new SearchResult { FilePath = filePath, LineNumber = lineNumber });
                        }
                    }

                    if (!dicLineNumbers.ContainsKey(filePath))
                        dicLineNumbers.Add(filePath, (UInt64)fileBytes.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file {filePath}: {ex.Message}");
            }
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
            _gLengthPreviewWidth = PreviewColumn.Width;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSearchPath_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbSearchPath.SelectedItem != null)
                SaveHistory(); // Save history whenever selection changes
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSearchText_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbSearchText.SelectedItem != null)
                SaveHistory(); // Save history whenever selection changes
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSearchExt_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbSearchExt.SelectedItem != null)
                SaveHistory(); // Save history whenever selection changes
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButPreview_Click(Object sender, RoutedEventArgs e)
        {
            if (PreviewColumn.Width.Value == 0) // If preview panel is hidden
            {
                SplitterColumn.Width = new GridLength(4); // Set width to show panel
                PreviewColumn.Width = _gLengthPreviewWidth; // Set width to show panel
                butPreview.Content = "<<<";
                
                ShowCurrentFileInPrewView();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridResults_OnSelectedCellsChanged(Object sender, SelectedCellsChangedEventArgs e)
        {
            ShowCurrentFileInPrewView();
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
                SaveHistory(); // Save updated history
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButSearch_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = cbSearchPath.Text; // Get folder path from ComboBox
            string searchTerm = cbSearchText.Text; // Get search term from ComboBox
            string fileExtensions = cbSearchExt.Text; // Get file extension from ComboBox

            bool bSubDirs = chbSubDirs.IsChecked!.Value;
            
            _pgWindow = new ProgressWindow();
            _pgWindow.Owner = this;
            _pgWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _pgWindow.Show();

            
            _bwSearch.RunWorkerAsync(new SearchArgs { Path = folderPath, Extensions = fileExtensions, SearchTerm = searchTerm, SubDirs = bSubDirs });
            butSearch.IsEnabled = false; // Disable the button to prevent multiple searches
            
            

            // Save history after search
            SaveHistory();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            SearchResult? sr = dgResult.SelectedItem as SearchResult;
            if (sr == null)
                return;
            
            if (File.Exists(sr.FilePath))
            {
                Process.Start(sr.FilePath);
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }

        private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            SearchResult? sr = dgResult.SelectedItem as SearchResult;
            if (sr == null)
                return;
            
            if (File.Exists(sr.FilePath))
            {
                string arguments = $"/select, \"{sr.FilePath}\"";
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
        private void ButDelHistory_Click(Object sender, RoutedEventArgs e)
        {
            vmSearch vm = DataContext as vmSearch;
            
            vm.SearchPaths.Clear();
            vm.SearchExtensions.Clear();
            vm.SearchTexts.Clear();
            
            File.Delete(HistoryFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(Object? sender, CancelEventArgs e)
        {
            vmSearch vm = DataContext as vmSearch;
            var jsonObject = new
            {
                MaxPreviewSize = vm.MaxPreviewSize
            };
            string json = JsonConvert.SerializeObject(jsonObject);
            // Save the JSON to a file
            File.WriteAllText(SettingsFilePath, json);
        }


        #endregion Events

    }

}