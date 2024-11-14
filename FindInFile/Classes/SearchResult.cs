using System.Collections.ObjectModel;

namespace FindInFiles.Classes;

/// <summary>
/// Item in the SearchRestulList in the GataGrid
/// </summary>
public class SearchResult
{
   public string FilePath { get; set; }
   public UInt64 FileSizeBytes { get; set; }
   public string FileSize { get; set; }
   public string Count { get; set; }
   public ObservableCollection<FoundItem> FoundItems { get; set; } = new();
   public bool IsSelected { get; set; }
}

public class FoundItem
{
   public int LineNumber { get; set; }
   public string Result { get; set; }
   public bool IsSelected { get; set; }
}