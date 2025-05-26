using System.Collections.ObjectModel;

namespace PlugInBase;

/// <summary>
/// Item in the SearchRestulList in the GataGrid
/// </summary>
public class SearchResultFile
{
   public string FilePath { get; set; }
   public UInt64 FileSizeBytes { get; set; }
   public string FileSize { get; set; }
   public string Count { get; set; }
   public bool IsSelected { get; set; }

   public ObservableCollection<FoundItem> FoundItems { get; set; } = new();
}

/// <summary>
/// An item of the the SearchResultFile
/// </summary>
public class FoundItem
{
   public int? LineNumber { get; set; }
   public int? Page { get; set; }
   public string Result { get; set; }
   public bool IsSelected { get; set; }
   
   public int? ParagraphNumber { get; set; }
   public string Sheet { get; set; }
   public string Column { get; set; }
   public int? Row { get; set; }
   
}