using System.Collections.ObjectModel;
using FindInFiles.Classes;

namespace FindInFiles.Model;

public class modelSearch
{
   public string SearchText { get; set; }
   public string Extension { get; set; }
   public string Path { get; set; }

   public bool SubDirs { get; set; }
   public ObservableCollection<string> SearchPaths { get; set; }
   public ObservableCollection<string> SearchExtensions { get; set; }
   public ObservableCollection<string> SearchTexts { get; set; }
   public UInt32 MaxPreViewSize { get; set; }
   public ObservableCollection<PlugInBase.SearchResult> SearchResultList { get; set; }

   public Int32 SearchMinMB { get; set; }
   public Int32 SearchMaxMB { get; set; }
   public Int32 WindowHeight { get; set; }
   public Int32 WindowWidth { get; set; }
   public Int32 MaxHistoryItems { get; set; }


   public modelSearch()
   {
      SearchPaths = new ObservableCollection<string>();
      SearchExtensions = new ObservableCollection<string>();
      SearchTexts = new ObservableCollection<string>();
      SearchResultList = new ObservableCollection<PlugInBase.SearchResult>();
   }

}