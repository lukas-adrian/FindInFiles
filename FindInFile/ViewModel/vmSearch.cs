using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using FindInFiles.Classes;
using FindInFiles.Extensions;
using FindInFiles.Model;
using Newtonsoft.Json;

namespace FindInFiles.ViewModel
{
   public class vmSearch : INotifyPropertyChanged 
   {
      
      #region Properties

      public string SearchText
      {
         get { return _model.SearchText; }
         set
         {
            if (_model.SearchText != value)
            {
               _model.SearchText = value;
               OnPropertyChanged();
            }
         }
      }

      public string Extension
      {
         get { return _model.Extension; }
         set
         {
            if (_model.Extension != value)
            {
               _model.Extension = value;
               OnPropertyChanged();
            }
         }
      }

      public string Path
      {
         get { return _model.Path; }
         set
         {
            if (_model.Path != value)
            {
               _model.Path = value;
               OnPropertyChanged();
            }
         }
      }

      public bool SubDirs
      {
         get { return _model.SubDirs; }
         set
         {
            if (_model.SubDirs != value)
            {
               _model.SubDirs = value;
               OnPropertyChanged();
            }
         }
      }
      
      public UInt32 MaxPreviewFileSizeMB
      {
         get { return _model.MaxPreViewSize; }
         set
         {
            if (_model.MaxPreViewSize != value)
            {
               _model.MaxPreViewSize = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchExtensions
      {
         get { return _model.SearchExtensions; }
         set
         {
            if (_model.SearchExtensions != value)
            {
               _model.SearchExtensions = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchPaths
      {
         get { return _model.SearchPaths; }
         set
         {
            if (_model.SearchPaths != value)
            {
               _model.SearchPaths = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<string> SearchTexts
      {
         get { return _model.SearchTexts; }
         set
         {
            if (_model.SearchTexts != value)
            {
               _model.SearchTexts = value;
               OnPropertyChanged();
            }
         }
      }
      
      public ObservableCollection<SearchResult> SearchResultList
      {
         get { return _model.SearchResultList; }
         set
         {
            if (_model.SearchResultList != value)
            {
               _model.SearchResultList = value;
               OnPropertyChanged();
            }
         }
      }
      
            
      public Int32 SearchMinMB
      {
         get { return _model.SearchMinMB; }
         set
         {
            if (_model.SearchMinMB != value)
            {
               try
               {
                  checked
                  {
                     _model.SearchMinMB = value < 0 ? 0 : value;
                  }
               }
               catch (OverflowException ex)
               {
                  // Handle the overflow exception here
                  _model.SearchMinMB = 0;
               }
               OnPropertyChanged();
            }
         }
      }
      
      public Int32 SearchMaxMB
      {
         get { return _model.SearchMaxMB; }
         set
         {
            try
            {
               checked
               {
                  _model.SearchMaxMB = value < 0 ? 0 : value;
               }
            }
            catch (OverflowException ex)
            {
               // Handle the overflow exception here
               _model.SearchMaxMB = 0;
            }
            OnPropertyChanged();
         }
      }
      
      public Int32 WindowHeight
      {
         get { return _model.WindowHeight; }
         set
         {
            if (_model.WindowHeight != value)
            {
               _model.WindowHeight = value;
               OnPropertyChanged();
            }
         }
      }
      
      public Int32 WindowWidth
      {
         get { return _model.WindowWidth; }
         set
         {
            if (_model.WindowWidth != value)
            {
               _model.WindowWidth = value;
               OnPropertyChanged();
            }
         }
      }

      public string Title { get; set; }
      public double MaxPreviewWidth { get; set; }
      public CancellationTokenSource CancellationTokenSource { get; set; }

      public int FilesCountAll { get; private set; }
      
      #endregion Properties
      
      private readonly modelSearch _model;
      private SearchHistory SearchHistory;
      
      public Dictionary<string, UInt64> dicLineNumbers = new();
      
      private readonly string SettingsFilePath;
      private readonly string HistoryFilePath;

      public ICommand SearchCommand { get; private set; }
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// 
      /// </summary>
      public vmSearch(string sSettingsFilePathIn, string sSHistoryFilePathIn)
      {
         _model = new modelSearch();
         SearchCommand = new RelayCommand(Execute, CanExecute);
         SettingsFilePath = sSettingsFilePathIn;
         HistoryFilePath = sSHistoryFilePathIn;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="propertyName"></param>
      protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
      
      #region ICommand
            
      public event EventHandler<int> ProgressChanged;
      public event EventHandler ProgressCompleted;
      
      /// <summary>
      /// 
      /// </summary>
      public async void Execute(object? parameter)
      {
         var progress = new Progress<int>(percent => 
         {
            ProgressChanged?.Invoke(this, percent); // Update progress bar in ProgressWindow
         });

         if (SearchMinMB > SearchMaxMB)
          SearchMinMB = 0;
         
         try
         {
            int outFilesCountAll = 0;
            List<SearchResult> results = await Task.Run(() => SearchInFolder(
               Path,
               Extension,
               SearchText,
               SubDirs,
               SearchMinMB,
               SearchMaxMB,
               progress,
               CancellationTokenSource.Token,
               out outFilesCountAll), CancellationTokenSource.Token);
            
            FilesCountAll = outFilesCountAll;
            SearchResultList = new ObservableCollection<SearchResult>(results);
         }
         catch (OperationCanceledException)
         {
            ProgressCompleted?.Invoke(this, EventArgs.Empty);
         }
         catch (Exception ex)
         {
            MessageBox.Show($"Error searching in folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ProgressCompleted?.Invoke(this, EventArgs.Empty);
         }
         finally
         {
            ProgressCompleted?.Invoke(this, EventArgs.Empty);
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="parameter"></param>
      /// <returns></returns>
      public  bool CanExecute(object parameter)
      {
         // Logic to determine if command can execute
         return true; // Example condition
      }

      #endregion ICommand

      #region Add/Remove Search Items
      
      /// <summary>
      /// 
      /// </summary>
      public void AddItemsSearchHistory(string text = "", string path = "", string extension = "")
      {
         if (!string.IsNullOrEmpty(text))
         {
            SearchHistory.Text.Remove(text);
            SearchHistory.Text.Insert(0, text);
            SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
         }
         if (!string.IsNullOrEmpty(extension))
         {
            SearchHistory.Extension.Remove(extension);
            SearchHistory.Extension.Insert(0, extension);
            SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         }
         if (!string.IsNullOrEmpty(path))
         {
            SearchHistory.Path.Remove(path);
            SearchHistory.Path.Insert(0, path);
            SearchPaths = new ObservableCollection<String>(SearchHistory.Path);
         }
         SaveHistory();

      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void AddItemToText(string item)
      {
         AddItemsSearchHistory(text: item);
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void AddItemToExt(string item)
      {
         AddItemsSearchHistory(extension: item);
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void AddItemToPath(string item)
      {
         AddItemsSearchHistory(path: item);
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void RemoveItemText(string item)
      {
         if (SearchHistory.Text.Contains(item))
         {
            SearchHistory.Text.Remove(item);
            SaveHistory();
            SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void RemoveItemExt(string item)
      {
         if (SearchHistory.Extension.Contains(item))
         {
            SearchHistory.Extension.Remove(item);
            SaveHistory();
            SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      public void RemoveItemPath(string item)
      {
         if (SearchHistory.Path.Contains(item))
         {
            SearchHistory.Path.Remove(item);

            SaveHistory();
            
            SearchPaths = new ObservableCollection<String>(SearchHistory.Path);
         }
      }
      #endregion Add/Remove Search Items

      #region SearchEngine

      /// <summary>
      /// 
      /// </summary>
      /// <param name="path"></param>
      /// <param name="extensions"></param>
      /// <param name="searchTerm"></param>
      /// <param name="subDirs"></param>
      /// <param name="maxFileSizeMB"></param>
      /// <param name="progress"></param>
      /// <param name="cancellationToken"></param>
      /// <param name="totalFilesAllOut"></param>
      /// <param name="minFileSizeMB"></param>
      /// <returns></returns>
      private List<SearchResult> SearchInFolder(
         string path,
         string extensions,
         string searchTerm,
         bool subDirs,
         int minFileSizeMB,
         int maxFileSizeMB,
         IProgress<int> progress,
         CancellationToken cancellationToken,
         out int totalFilesAllOut)
      {
         dicLineNumbers = new Dictionary<String, UInt64>();
         var foundResults = new List<SearchResult>();
         var lstExtensions = extensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim()).ToList(); // Trim whitespace

         totalFilesAllOut = 0;
         try
         {

            int processedFiles = 0;
            
            List<String> lstAllFiles = new();
            foreach (String extension in lstExtensions)
            {
               if (subDirs)
                  lstAllFiles.AddRange(Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.AllDirectories).ToList());
               else
                  lstAllFiles.AddRange(Directory.EnumerateFiles(path, $"*.{extension}", SearchOption.TopDirectoryOnly).ToList());
            }
 
            foreach (String file in lstAllFiles)
            {
               
               FileInfo fileInfo = new(file);

               int FileSizeMB = Convert.ToInt32(fileInfo.Length / (1024.0 * 1024.0));
               if (!(minFileSizeMB == 0 && maxFileSizeMB == 0) &&
                   (FileSizeMB < minFileSizeMB || FileSizeMB > maxFileSizeMB))
               {
                  continue;
               }
               
               totalFilesAllOut++;
               
               if (cancellationToken.IsCancellationRequested)
               {
                  progress?.Report(0);
                  throw new OperationCanceledException();
               }
               
               
               SearchResult? foundInFile = FileContainsTermUsingBytes(file, searchTerm);
               if (foundInFile != null)
               {
                  foundInFile.FileSizeBytes = Convert.ToUInt64(fileInfo.Length);
                  foundInFile.FileSize = $"Size: {Convert.ToUInt64(fileInfo.Length / 1024.0)} (kB)";
                  foundResults.Add(foundInFile);
               }
               
               processedFiles++;
               int percentage = (int)(processedFiles / (float)lstAllFiles.Count * 100);
               progress?.Report(percentage);
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
      /// <param name="filePath"></param>
      /// <param name="searchTerm"></param>
      private SearchResult? FileContainsTermUsingBytes(string filePath, string searchTerm)
      {
         SearchResult? foundResults = null;

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
                  int nFoundIndex = line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
                  if (nFoundIndex >= 0)
                  {
                     if (foundResults == null)
                     {
                        foundResults = new SearchResult
                        {
                           FilePath = filePath,
                        };
                     }

                     FoundItem item = new FoundItem();
                     item.LineNumber = lineNumber;
                     item.Result = line.Trim();
                     
                     foundResults.FoundItems.Add(item);
                  }

                  if(foundResults != null)
                     foundResults.Count = $"Count: {Convert.ToUInt32(foundResults.FoundItems.Count)}";
               }

               if (!dicLineNumbers.ContainsKey(filePath))
                  dicLineNumbers.Add(filePath, (UInt64)fileBytes.Length);
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show($"Error reading file {filePath}: {ex.Message}");
         }

         return foundResults;
      }
      
      #endregion SearchEngine
      
      #region Save/Load/Delete user files
      
      /// <summary>
      /// 
      /// </summary>
      public void SaveHistory()
      {
         if (!File.Exists(HistoryFilePath))
         {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(HistoryFilePath));
            SearchHistory = new SearchHistory();
         }
         
         var updatedJson = JsonConvert.SerializeObject(SearchHistory, Formatting.Indented);
         File.WriteAllText(HistoryFilePath, updatedJson); // Save to file
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void LoadHistory()
      {
         // Clear existing items before loading new history
         SearchPaths.Clear();
         SearchExtensions.Clear();
         SearchTexts.Clear();

         if (File.Exists(HistoryFilePath))
         {
            var json = File.ReadAllText(HistoryFilePath);
            SearchHistory = JsonConvert.DeserializeObject<SearchHistory>(json);

            if (SearchHistory != null)
            {

               SearchPaths.AddRange(SearchHistory.Path.Distinct().ToList());
               SearchExtensions.AddRange(SearchHistory.Extension.Distinct().ToList());
               SearchTexts.AddRange(SearchHistory.Text.Distinct().ToList());
            }
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void LoadSettings()
      {
         if (!File.Exists(SettingsFilePath))
         {//Default value
            MaxPreviewFileSizeMB = 5;
            SubDirs = false;
            SearchMinMB = 0;
            SearchMaxMB = 0;
            MaxPreviewWidth = 400;
         }
         else
         {
            
            string json = File.ReadAllText(SettingsFilePath);
            Settings settings = JsonConvert.DeserializeObject<Settings>(json);

            MaxPreviewFileSizeMB = settings.MaxPreviewSize;
            SubDirs = settings.SubDirs;
            SearchMinMB = settings.SearchMinMB;
            SearchMaxMB = settings.SearchMaxMB;
            MaxPreviewWidth = settings.MaxPreviewWidth;
            WindowHeight = settings.WindowHeight;
            WindowWidth = settings.WindowWidth;

         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void SaveSettings()
      {
         Settings settings = new()
         {
            MaxPreviewSize = MaxPreviewFileSizeMB,
            SubDirs = SubDirs,
            SearchMinMB = SearchMinMB,
            SearchMaxMB = SearchMaxMB,
            MaxPreviewWidth = MaxPreviewWidth,
            WindowHeight = WindowHeight,
            WindowWidth = WindowWidth,
         };

         string json = JsonConvert.SerializeObject(settings);
         // Save the JSON to a file
         File.WriteAllText(SettingsFilePath, json);
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void DeleteHistory()
      {  
                    
         SearchPaths.Clear();
         SearchExtensions.Clear();
         SearchTexts.Clear();
            
         File.Delete(HistoryFilePath);
      }
      
      public class Settings
      {
         public uint MaxPreviewSize { get; set; }
         public bool SubDirs { get; set; }
         public int SearchMinMB { get; set; }
         public int SearchMaxMB { get; set; }
         
         [DefaultValue(400)]            
         [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
         public double MaxPreviewWidth { get; set; }
         
         [DefaultValue(600)]            
         [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
         public int WindowHeight { get; set; }
         
         [DefaultValue(600)]            
         [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
         public int WindowWidth { get; set; }
      }
      
      #endregion Save/Load user files
   }
}