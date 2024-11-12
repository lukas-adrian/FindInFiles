using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using FindInFile.Classes;
using FindInFile.Model;
using Newtonsoft.Json;

namespace FindInFile.ViewModel
{
   public class vmSearch : INotifyPropertyChanged 
   {
      
      #region Properties

      public string Title
      {
         get { return _model.Title; }
         set
         {
            if (_model.Title != value)
            {
               _model.Title = value;
               OnPropertyChanged();
            }
         }
      }

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
      
      public UInt32 MaxPreviewSize
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
      
      public CancellationTokenSource CancellationTokenSource
      {
         get => _cancellationTokenSource;
         set => _cancellationTokenSource = value;
      }
      
      #endregion Properties
      
      private readonly modelSearch _model;
      private SearchHistory SearchHistory;
      
      public Dictionary<string, UInt64> dicLineNumbers = new();
      
      private readonly string SettingsFilePath;
      private readonly string HistoryFilePath;

      public ICommand SearchCommand { get; private set; }
      private CancellationTokenSource _cancellationTokenSource;
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
      /// <param name="parameter"></param>
      public async void Execute(object parameter)
      {

         if (parameter is not SearchArgs args) return;
         
          var progress = new Progress<int>(percent => 
          {
             ProgressChanged?.Invoke(this, percent); // Update progress bar in ProgressWindow
          });
         
         try
         {
            var results = await Task.Run(() => SearchInFolder(args.Path, args.Extensions, args.SearchTerm, args.SubDirs, progress, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
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
            SearchHistory.Text.Add(text);
            SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
         }
         if (!string.IsNullOrEmpty(extension))
         {
            SearchHistory.Extension.Add(extension);
            SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         }
         if (!string.IsNullOrEmpty(path))
         {
            SearchHistory.Path.Add(path);
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
      /// <param name="progress"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      public List<SearchResult> SearchInFolder(string path, string extensions, string searchTerm, bool subDirs, IProgress<int> progress, CancellationToken cancellationToken)
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
                  if (cancellationToken.IsCancellationRequested)
                  {
                     progress?.Report(0);
                     throw new OperationCanceledException();
                  }
                  
                  FileContainsTermUsingBytes(file, searchTerm, foundResults);
                  processedFiles++;
                  int percentage = (int)((processedFiles / (float)totalFiles) * 100);
                  progress?.Report(percentage);
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
      /// <param name="filePath"></param>
      /// <param name="searchTerm"></param>
      /// <param name="foundResults"></param>
      private void FileContainsTermUsingBytes(string filePath, string searchTerm, List<SearchResult> foundResults)
      {
         try
         {
            FileInfo fileInfo = new(filePath);
            
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
                     foundResults.Add(new SearchResult { FilePath = filePath, LineNumber = lineNumber, FileSize = Convert.ToUInt64(fileInfo.Length / 1024.0)});
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

               foreach (string sPath in SearchHistory.Path)
               {
                  SearchPaths.Add(sPath);
               }
               foreach (string sExt in SearchHistory.Extension)
               {
                  SearchExtensions.Add(sExt);
               }
               foreach (string sText in SearchHistory.Text)
               {
                  SearchTexts.Add(sText);
               }
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
            MaxPreviewSize = 5;
         }
         else
         {
            string json = File.ReadAllText(SettingsFilePath);
            var jsonObject = JsonConvert.DeserializeAnonymousType(json, new { MaxPreviewSize = 0 });
            MaxPreviewSize = Convert.ToUInt32(jsonObject.MaxPreviewSize);
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void SaveSettings()
      {
         var jsonObject = new
         {
            MaxPreviewSize = MaxPreviewSize
         };
         string json = JsonConvert.SerializeObject(jsonObject);
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
      
      #endregion Save/Load user files
   }
}