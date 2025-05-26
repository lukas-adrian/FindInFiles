using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using PlugInBase;

namespace FindInFiles.Classes
{
   public static class Helper
   {
      public static Tuple<SearchResultFile, FoundItem?>? GetCurrentTreeViewItem(TreeView tvControl, object selectedItem)
      {
         if (selectedItem is FoundItem foundItem)
         {
            // Find the parent SearchResult
            foreach (SearchResultFile searchResult in (tvControl.ItemsSource as ObservableCollection<SearchResultFile>)!)
            {
               if (searchResult.FoundItems.Contains(foundItem))
               {
                  return new Tuple<SearchResultFile, FoundItem?>(searchResult, foundItem);
               }
            }
         }
         else if (selectedItem is SearchResultFile searchResult)
         {
            return new Tuple<SearchResultFile, FoundItem?>(searchResult, null);
         }

         return null;
      }

      /// <summary>
      /// Fast method - checks first N bytes for binary content
      /// </summary>
      public static bool IsTextFile(string filePath, int bytesToCheck = 1024)
      {
         try
         {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
               var buffer = new byte[Math.Min(bytesToCheck, fileStream.Length)];
               int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

               return IsTextContent(buffer, bytesRead);
            }
         }
         catch
         {
            return false;
         }
      }

      /// <summary>
      /// Checks if byte array contains text content
      /// </summary>
      private static bool IsTextContent(byte[] data, int length)
      {
         if (length == 0) return true; // Empty file is considered text

         int binaryBytes = 0;
         int totalBytes = 0;

         for (int i = 0; i < length; i++)
         {
            byte b = data[i];
            totalBytes++;

            // Check for null bytes (strong indicator of binary)
            if (b == 0)
            {
               binaryBytes++;
            }
            // Check for control characters (except common text ones)
            else if (b < 32 && b != 9 && b != 10 && b != 13) // Tab, LF, CR are OK
            {
               binaryBytes++;
            }
            // Check for high-bit characters that might indicate binary
            else if (b > 127)
            {
               // Could be UTF-8 or other encoding, be more lenient
               // Only count as binary if it's not valid UTF-8 sequence start
               if (b >= 240) // Start of 4-byte UTF-8 sequence
               {
                  // Check if next bytes form valid UTF-8
                  if (!IsValidUtf8Sequence(data, i, length))
                     binaryBytes++;
               }
            }
         }

         // If more than 10% of bytes are "binary", consider it a binary file
         double binaryRatio = (double)binaryBytes / totalBytes;
         return binaryRatio <= 0.10;
      }

      /// <summary>
      /// Simple UTF-8 validation helper
      /// </summary>
      private static bool IsValidUtf8Sequence(byte[] data, int startIndex, int length)
      {
         // Simplified UTF-8 validation - just check basic patterns
         if (startIndex >= length) return false;

         byte first = data[startIndex];

         // Single byte (ASCII)
         if (first <= 127) return true;

         // Multi-byte sequences
         int expectedBytes = 0;
         if ((first & 0xE0) == 0xC0) expectedBytes = 2; // 110xxxxx
         else if ((first & 0xF0) == 0xE0) expectedBytes = 3; // 1110xxxx
         else if ((first & 0xF8) == 0xF0) expectedBytes = 4; // 11110xxx
         else return false;

         // Check if we have enough bytes
         if (startIndex + expectedBytes > length) return false;

         // Check continuation bytes
         for (int i = 1; i < expectedBytes; i++)
         {
            if ((data[startIndex + i] & 0xC0) != 0x80) // Should be 10xxxxxx
               return false;
         }

         return true;
      }
   }
}