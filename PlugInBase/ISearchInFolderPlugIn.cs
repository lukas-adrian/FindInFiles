namespace PlugInBase
{
   /// <summary>
   /// Interface for any search plugin
   /// </summary>
   public interface ISearchInFolderPlugIn
   {
   
      /// <summary>some ID that every plugin is individual</summary>
      public Guid ID { get; }
   
      /// <summary>Name and description for the plugin</summary>
      public string Name { get; }
   
      /// <summary>Name and description for the plugin</summary>
      public string Description { get; }

      /// <summary>List of extension that the plugin can work with</summary>
      public List<string> Extensions { get; }
   
      /// <summary>custom debug output if needed</summary>
      event EventHandler<string> DebugOutput;
      /// <summary>results of the search</summary>
      event EventHandler<FileSearchEventArgs> FileSearchCompleted;
         
      /// <summary>starts the search</summary>
      public Task<List<FileSearchEventArgs>> SearchInFolder(
         List<String> lstAllFiles,
         String searchTerm,
         bool matchCase,
         bool wholeWord,
         IProgress<Int32> progress,
         CancellationToken cancellationToken);
   }
}