using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using FindInFiles.Classes;
using FindInFiles.Classes.PlugInLoader;
using FindInFiles.Extensions;
using FindInFiles.Model;
using Newtonsoft.Json;
using PlugInBase;
using Serilog;

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
      
      public bool WholeWord
      {
         get { return _model.WholeWord; }
         set
         {
            if (_model.WholeWord != value)
            {
               _model.WholeWord = value;
               OnPropertyChanged();
            }
         }
      }
      
      public bool MatchCase
      {
         get { return _model.MatchCase; }
         set
         {
            if (_model.MatchCase != value)
            {
               _model.MatchCase = value;
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
      
      public ObservableCollection<PlugInBase.SearchResultFile> SearchResultList
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

      public Int32 MaxHistoryItems
      {
         get { return _model.MaxHistoryItems; }
         set
         {
            try
            {
               checked
               {
                  _model.MaxHistoryItems = value < 0 ? 0 : value;
               }
            }
            catch (OverflowException ex)
            {
               // Handle the overflow exception here
               _model.MaxHistoryItems = 0;
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

      private ILogger Log = Serilog.Log.ForContext<vmSearch>();
      
      public event PropertyChangedEventHandler PropertyChanged;
      public ICommand SearchCommand { get; private set; }
      public Dictionary<string, UInt64> dicLineNumbers = new();


      private readonly modelSearch _model;
      private readonly string SettingsFilePath;
      private readonly string HistoryFilePath;
      private SearchHistory SearchHistory;
      private readonly Dictionary<String, ISearchInFolderPlugIn> _dicExtensionPlugin;
      public Dictionary<String, IPreviewPlugIn> _dicPrewviewPlugin { get; set; }
      private ConcurrentBag<PlugInBase.SearchResultFile> SearchResultListTmp;
      private readonly object _lock = new object();

      /// <summary>
      /// 
      /// </summary>
      public vmSearch(string sSettingsFilePathIn, string sHistoryFilePathIn)
      {
         Log.Information("Init Search Class");
         
         _model = new modelSearch();
         SearchCommand = new RelayCommand(Execute, CanExecute);
         SettingsFilePath = sSettingsFilePathIn;
         HistoryFilePath = sHistoryFilePathIn;

         _dicExtensionPlugin = new Dictionary<String, ISearchInFolderPlugIn>();
         _dicPrewviewPlugin = new Dictionary<String, IPreviewPlugIn>();

         string sPlugInFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "PlugIns");
         
         (List<ISearchInFolderPlugIn> search, List<IPreviewPlugIn> preview) PlugIns = PluginLoader.LoadPlugins(sPlugInFolder);
 
         Log.Information("prepare plugins");
         foreach (ISearchInFolderPlugIn plugIn in PlugIns.search)
         {
            List<string> lstExt = plugIn.Extensions;
            plugIn.FileSearchCompleted += PlugInOnFileSearchCompleted;
            plugIn.DebugOutput += PlugInOnDebugOutput;

            if (lstExt.Count == 0)
            {//default
               _dicExtensionPlugin.Add("", plugIn);
            }
            else
            {
               foreach (String sExt in lstExt)
               {
                  if (_dicExtensionPlugin.TryGetValue(sExt.ToUpper(), out ISearchInFolderPlugIn plugInOut))
                  {
#if DEBUG
                     Log.Debug("Extension {ext} exists already in '{plugin}' (ISearchInFolderPlugIn)", sExt, plugInOut.Name);
#endif
                  }
                  else
                  {
                     _dicExtensionPlugin.Add(sExt.ToUpper(), plugIn);
                  }
               }
            }
         }
         
         foreach (IPreviewPlugIn preveiw in PlugIns.preview)
         {
            List<string> lstExt = preveiw.Extensions;

            if (lstExt.Count == 0)
            {//default
               _dicPrewviewPlugin.Add("", preveiw);
            }
            else
            {
               foreach (String sExt in lstExt)
               {
                  if (_dicPrewviewPlugin.TryGetValue(sExt.ToUpper(), out IPreviewPlugIn plugInOut))
                  {
#if DEBUG
                     Log.Debug("Extension {ext} exists already in '{plugin}' (IPreviewPlugIn)", sExt, plugInOut.Name);
#endif
                  }
                  else
                  {
                     _dicPrewviewPlugin.Add(sExt.ToUpper(), preveiw);
                  }
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PlugInOnDebugOutput(Object? sender, String e)
      {
#if DEBUG
         Log.Verbose("DebugOutput = {e}", e);
#endif
      }
      
      private void PlugInOnFileSearchCompleted(Object? sender, FileSearchEventArgs e)
      {
         Log.Information("search complete");
         lock (_lock)
         {
            if (e.EventStatus == FileSearchEventArgs.Status.Completed)
            {
               List<PlugInBase.SearchResultFile> results = e.ResultList;
               FilesCountAll += e.totalFilesAllOut.Value;

               foreach (PlugInBase.SearchResultFile result in results)
               {
                  SearchResultListTmp.Add(result);
               }
            }
         }
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
         Log.Information("start search");
         
         SearchResultListTmp = new ConcurrentBag<SearchResultFile>();
         SearchResultList = new ObservableCollection<SearchResultFile>();
         FilesCountAll = 0;

         var progress = new Progress<int>(percent => 
         {
            ProgressChanged?.Invoke(this, percent); // Update progress bar in ProgressWindow
         });

         if (SearchMinMB > SearchMaxMB)
            SearchMinMB = 0;
         
         try
         {
            Log.Debug("getting extensions");
            var lstExtensionPairs = Extension
               .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(p => p.Trim())
               .Select(p =>
               {
                  string pattern = p;
                  string ext = "";

                  if (string.IsNullOrWhiteSpace(p)) return (pattern, ext);

                  if (p.StartsWith("*.")) ext = p.Substring(1);                  // "*.txt" -> ".txt"
                  else if (p.StartsWith(".")) ext = p;                           // ".pdf" -> ".pdf"
                  else if (p.Contains('.') && !p.Contains('*'))
                     ext = "." + p.Split('.').Last();                           // "myFile.txt" -> ".txt"
                  else ext = "";                                                 // e.g., "README", "data*", "*.*"

                  return (pattern, ext);
               })
               .Distinct()
               .ToList();


            List<Task> tasks = new();

#if DEBUG
            Log.Debug("seraching for search plugins");
#endif
            Dictionary<Guid, (ISearchInFolderPlugIn, List<String>)> dicPlugInSearch = new();
            foreach (var (sPattern, sExt) in lstExtensionPairs)
            {
               List<string> lstFilesChecked = new();
               DirectoryInfo dirPath = new DirectoryInfo(Path);
               SearchOption searchOption = SubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
               foreach (FileInfo fi in dirPath.EnumerateFiles(sPattern, searchOption))
               {
                  if (ValidateFileSize(fi, SearchMinMB, SearchMaxMB))
                  {
                     lstFilesChecked.Add(fi.FullName);
                  }
               }
               ISearchInFolderPlugIn plugIn;
               if (_dicExtensionPlugin.TryGetValue(sExt.ToUpper(), out plugIn))
               {
#if DEBUG
                  Log.Verbose("found serach plugin for ext {sExt}", sExt);
#endif
                  if(!dicPlugInSearch.ContainsKey(plugIn.ID))
                     dicPlugInSearch.Add(plugIn.ID, (plugIn, lstFilesChecked));
               }
               else if (_dicExtensionPlugin.TryGetValue("", out plugIn))
               {//default
#if DEBUG
                  Log.Verbose("using default serach plugin for ext {sExt}", sExt);
#endif
                  if(!dicPlugInSearch.ContainsKey(plugIn.ID))
                     dicPlugInSearch.Add(plugIn.ID, (plugIn, lstFilesChecked));
               }
               else
               {
                  throw new NotImplementedException($"no PlugIn loaeded | 20241120-112231");
               }
            }


            foreach (KeyValuePair<Guid, (ISearchInFolderPlugIn plugIn, List<String> Files)> kvp in dicPlugInSearch)
            {
               tasks.Add(Task.Run(() => kvp.Value.plugIn.SearchInFolder(
                  kvp.Value.Files,
                  SearchText,
                  MatchCase,
                  WholeWord,
                  progress,
                  CancellationTokenSource.Token), CancellationTokenSource.Token));
            }

            await Task.WhenAll(tasks);

            foreach (SearchResultFile result in SearchResultListTmp)
            {
               SearchResultList.Add(result);
            }
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
      /// <param name="fileInfo"></param>
      /// <param name="minFileSizeMBIn"></param>
      /// <param name="maxFileSizeMBIn"></param>
      /// <returns></returns>
      private bool ValidateFileSize(
         FileInfo fileInfo,
         Int32 minFileSizeMBIn = 0,
         Int32 maxFileSizeMBIn = 0)
      {
         if (minFileSizeMBIn == 0 && maxFileSizeMBIn == 0)
            return true;

         int fileSizeMB = Convert.ToInt32(fileInfo.Length / (1024.0 * 1024.0));

         if (!(minFileSizeMBIn == 0 && maxFileSizeMBIn == 0) &&
             (fileSizeMB < minFileSizeMBIn || fileSizeMB > maxFileSizeMBIn))
         {
#if DEBUG
            Log.Verbose("File in ValidateFileSize ignored {file}", fileInfo.Name);
#endif
            return false;
         }

         return true;
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
         Log.Information("adding an item into the history");
         if (!string.IsNullOrEmpty(text))
         {
            SearchHistory.Text.Remove(text);
            SearchHistory.Text.Insert(0, text);
            while (SearchHistory.Text.Count > MaxHistoryItems)
            {
               SearchHistory.Text.RemoveAt(MaxHistoryItems);
            }

            SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
         }
         if (!string.IsNullOrEmpty(extension))
         {
            SearchHistory.Extension.Remove(extension);
            SearchHistory.Extension.Insert(0, extension);
            while (SearchHistory.Extension.Count > MaxHistoryItems)
            {
               SearchHistory.Extension.RemoveAt(MaxHistoryItems);
            }
            SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         }
         if (!string.IsNullOrEmpty(path))
         {
            SearchHistory.Path.Remove(path);
            SearchHistory.Path.Insert(0, path);
            while (SearchHistory.Path.Count > MaxHistoryItems)
            {
               SearchHistory.Path.RemoveAt(MaxHistoryItems);
            }
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
         Log.Information("removing a text item from the history");
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
         Log.Information("removing an extension item from the history");
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
         Log.Information("removing a path item from the history");
         if (SearchHistory.Path.Contains(item))
         {
            SearchHistory.Path.Remove(item);

            SaveHistory();
            
            SearchPaths = new ObservableCollection<String>(SearchHistory.Path);
         }
      }
      #endregion Add/Remove Search Items
      
      #region Save/Load/Delete user files
      
      /// <summary>
      /// 
      /// </summary>
      public void SaveHistory()
      {
         Log.Information("saving history");
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
         Log.Information("loading history");
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
         Log.Information("loading settings");
         if (!File.Exists(SettingsFilePath))
         {//Default value
            MaxPreviewFileSizeMB = 5;
            SubDirs = false;
            WholeWord = false;
            MatchCase = false;
            SearchMinMB = 0;
            SearchMaxMB = 0;
            MaxPreviewWidth = 400;
            MaxHistoryItems = 10;
            SearchText = "";
            Extension = "";
            Path = "";
         }
         else
         {
            
            string json = File.ReadAllText(SettingsFilePath);
            Settings settings = JsonConvert.DeserializeObject<Settings>(json);

            MaxPreviewFileSizeMB = settings.MaxPreviewSize;
            SubDirs = settings.SubDirs;
            WholeWord = settings.WholeWord;
            MatchCase = settings.MatchCase;
            SearchMinMB = settings.SearchMinMB;
            SearchMaxMB = settings.SearchMaxMB;
            MaxPreviewWidth = settings.MaxPreviewWidth;
            WindowHeight = settings.WindowHeight;
            WindowWidth = settings.WindowWidth;
            MaxHistoryItems = settings.MaxHistoryItems;
            SearchText = settings.SearchText;
            Extension = settings.Extension;
            Path = settings.Path;

         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void SaveSettings()
      {
         Log.Information("saving settings");
         JsonSerializerSettings jsonSettings = new()
         {
            Formatting = Formatting.Indented
         };

         Settings settings = new()
         {
            MaxPreviewSize = MaxPreviewFileSizeMB,
            SubDirs = SubDirs,
            WholeWord = WholeWord,
            MatchCase = MatchCase,
            SearchMinMB = SearchMinMB,
            SearchMaxMB = SearchMaxMB,
            MaxPreviewWidth = MaxPreviewWidth,
            WindowHeight = WindowHeight,
            WindowWidth = WindowWidth,
            MaxHistoryItems = MaxHistoryItems,
            SearchText = SearchText,
            Extension = Extension,
            Path = Path,
         };

         string json = JsonConvert.SerializeObject(settings, jsonSettings);
         // Save the JSON to a file
         File.WriteAllText(SettingsFilePath, json);
      }
      
      /// <summary>
      /// 
      /// </summary>
      public void DeleteHistory()
      {  
         Log.Information("delete whole history");  
         SearchPaths.Clear();
         SearchExtensions.Clear();
         SearchTexts.Clear();
            
         File.Delete(HistoryFilePath);
      }
      
      public class Settings
      {
         public uint MaxPreviewSize { get; set; }
         public bool SubDirs { get; set; }
         public bool WholeWord { get; set; }
         public bool MatchCase { get; set; }
         public int SearchMinMB { get; set; }
         public int SearchMaxMB { get; set; }
         public string SearchText { get; set; }
         public string Extension { get; set; }
         public string Path { get; set; }

         [DefaultValue(10)]
         [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
         public int MaxHistoryItems { get; set; }

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