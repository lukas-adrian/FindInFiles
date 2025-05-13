namespace PlugInBase
{
   public interface ISearchInFolderPlugIn
   {
      event EventHandler<string> DebugOutput;
      event EventHandler<FileSearchEventArgs> FileSearchCompleted;

      public List<string> GetExtensions();
         
      public Task<List<FileSearchEventArgs>> SearchInFolder(
         List<String> lstAllFiles,
         String searchTerm,
         bool matchCase,
         bool wholeWord,
         IProgress<Int32> progress,
         CancellationToken cancellationToken);

      void OnFileSearchCompleted(FileSearchEventArgs e);
      void OnDebugOutput(string e);
   }
}