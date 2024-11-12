namespace FindInFile.Classes;

/// <summary>
/// Item in the SearchRestulList in the GataGrid
/// </summary>
public class SearchResult
{
   public string FilePath { get; set; }
   public int LineNumber { get; set; }
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
