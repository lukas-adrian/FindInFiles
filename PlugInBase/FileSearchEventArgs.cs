using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugInBase
{

   /// <summary>
   /// Eventargs for the search interface events
   /// </summary>
   public class FileSearchEventArgs : EventArgs
   {

      public enum Status
      {
         NotDefined = 0,
         Completed = 1,
         Error = 2,
      }
      
      public Status EventStatus { get; private set; }

      /// <summary>list with the search results</summary>
      public List<SearchResultFile>? ResultList { get; set; }

      /// <summary>shows the path of the file and the full amount of line numbers</summary>
      public Dictionary<String, UInt64>? dicLineNumbers { get; set; }

      /// <summary>all searched files</summary>
      public Int32? totalFilesAllOut { get; set; }
      public string? Value { get; private set; }


      public FileSearchEventArgs(
         Status eventStatus,
         List<SearchResultFile>? resultList = null,
         Dictionary<String, UInt64>? dicLineNumbers = null,
         Int32? totalFilesAllOut = null,
         string? value = null
      )
      {
         EventStatus = eventStatus;
         ResultList = resultList;
         this.dicLineNumbers = dicLineNumbers;
         this.totalFilesAllOut = totalFilesAllOut;
         this.Value = value;
      }
   }
}