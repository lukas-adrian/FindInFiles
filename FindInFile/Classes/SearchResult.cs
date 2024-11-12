using System.Collections.ObjectModel;

namespace FindInFile.Classes;

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

/// <summary>
/// Arguments to search some files
/// </summary>
public class SearchArgs
{
   public string Path { get; set; }
   public string Extensions { get; set; }
   public string SearchTerm { get; set; }
   public bool SubDirs { get; set; }
}
