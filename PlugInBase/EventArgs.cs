using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugInBase
{
   public class FileSearchEventArgs : EventArgs
   {

      public enum Status
      {
         NotDefined = 0,
         Completed = 1,
         Error = 2,
      }

      public Status EventStatus { get; private set; }
      public List<SearchResultFile>? ResultList { get; set; }
      public Dictionary<String, UInt64>? dicLineNumbers { get; set; }
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