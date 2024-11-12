using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using FindInFile.Classes;
using FindInFile.Model;
using Newtonsoft.Json;

namespace FindInFile.ViewModel
{
   public class vmSearch : INotifyPropertyChanged
   {
      private modelSearch _model;

      public vmSearch()
      {
         _model = new modelSearch();
      }

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
      
      public string HistoryFilePath
      {
         get { return _model.HistoryFilePath; }
         set
         {
            if (_model.HistoryFilePath != value)
            {
               _model.HistoryFilePath = value;
               OnPropertyChanged();
            }
         }
      }

      private SearchHistory SearchHistory;
      public Dictionary<string, UInt64> dicLineNumbers = new();

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
               foreach (string sPath in SearchHistory.Extension)
               {
                  SearchExtensions.Add(sPath);
               }
               foreach (string sPath in SearchHistory.Text)
               {
                  SearchTexts.Add(sPath);
               }
            }
         }
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="sPathIn"></param>
      /// <param name="sExtensionIn"></param>
      /// <param name="sTextIn"></param>
      public void SaveHistory(string sPathIn, string sExtensionIn, string sTextIn)
      {
         if (File.Exists(HistoryFilePath))
         {
            var json = File.ReadAllText(HistoryFilePath);
            SearchHistory = JsonConvert.DeserializeObject<SearchHistory>(json) ?? new SearchHistory();
         }
         else
         {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(HistoryFilePath));
            SearchHistory = new SearchHistory();
         }

         SearchHistory.Path.Add(sPathIn);
         SearchHistory.Extension.Add(sExtensionIn);
         SearchHistory.Text.Add(sTextIn);
            
         var updatedJson = JsonConvert.SerializeObject(SearchHistory, Formatting.Indented);
         File.WriteAllText(HistoryFilePath, updatedJson); // Save to file
         
         SearchPaths = new ObservableCollection<String>(SearchHistory.Path);
         SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
      }
      
      public void RemoveItemText(string item)
      {
         if (SearchHistory.Text.Contains(item))
         {
            SearchHistory.Text.Remove(item);
            
            SaveHistory();
            
            SearchTexts = new ObservableCollection<String>(SearchHistory.Text);
         }
      }
      
      public void RemoveItemExt(string item)
      {
         if (SearchHistory.Extension.Contains(item))
         {
            SearchHistory.Extension.Remove(item);
            
            SaveHistory();
            
            SearchExtensions = new ObservableCollection<String>(SearchHistory.Extension);
         }
      }
      
      public void RemoveItemPath(string item)
      {
         if (SearchHistory.Path.Contains(item))
         {
            SearchHistory.Path.Remove(item);

            SaveHistory();
            
            SearchPaths = new ObservableCollection<String>(SearchHistory.Path);
         }
      }

      private void SaveHistory()
      {
         var updatedJson = JsonConvert.SerializeObject(SearchHistory, Formatting.Indented);
         File.WriteAllText(HistoryFilePath, updatedJson); // Save to file
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="path"></param>
      /// <param name="extensions"></param>
      /// <param name="searchTerm"></param>
      /// <param name="subDirs"></param>
      /// <param name="worker"></param>
      /// <returns></returns>
      public List<SearchResult> SearchInFolder(string path, string extensions, string searchTerm, bool subDirs, BackgroundWorker? worker)
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
      
              
      /// <summary>
      /// 
      /// </summary>
      public void LoadSettings(string sSettingsFilePath)
      {
         if (!File.Exists(sSettingsFilePath))
         {//Default value
            MaxPreviewSize = 5;
         }
         else
         {
            string json = File.ReadAllText(sSettingsFilePath);
            var jsonObject = JsonConvert.DeserializeAnonymousType(json, new { MaxPreviewSize = 0 });
            MaxPreviewSize = Convert.ToUInt32(jsonObject.MaxPreviewSize);
         }
      }


   }
}