using System.Collections.ObjectModel;
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
   }
}