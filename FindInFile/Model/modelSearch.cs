using System.Collections.ObjectModel;

namespace FindInFile.Model;

public class modelSearch
{
   public string SearchText { get; set; }
   public string Extension { get; set; }
   public string Path { get; set; }
   public string HistoryFilePath { get; set; }

   public bool SubDirs { get; set; }
   public ObservableCollection<string> SearchPaths { get; set; }
   public ObservableCollection<string> SearchExtensions { get; set; }
   public ObservableCollection<string> SearchTexts { get; set; }
   public UInt32 MaxPreViewSize { get; set; }
   public string Title { get; set; }


   public modelSearch()
   {
      SearchPaths = new ObservableCollection<string>();
      SearchExtensions = new ObservableCollection<string>();
      SearchTexts = new ObservableCollection<string>();
   }

}