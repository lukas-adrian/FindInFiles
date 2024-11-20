namespace PlugInBase
{
   public interface ISearchInFolderPlugIn
   {
      event EventHandler<FileSearchEventArgs> FileSearchCompleted;

      public List<string> GetExtensions();

      public Task<List<FileSearchEventArgs>> SearchInFolder(
         string path,
         string extensions,
         string searchTerm,
         bool subDirs,
         int minFileSizeMB,
         int maxFileSizeMB,
         IProgress<int> progress,
         CancellationToken cancellationToken);

      void OnFileSearchCompleted(FileSearchEventArgs e);
   }
}
