﻿using System.Collections.ObjectModel;
using System.Windows.Controls;
using PlugInBase;

namespace FindInFiles.Classes
{
   public static class Helper
   {
      public static Tuple<SearchResult, FoundItem?>? GetCurrentTreeViewItem(TreeView tvControl, object selectedItem)
      {
         if (selectedItem is FoundItem foundItem)
         {
            // Find the parent SearchResult
            foreach (SearchResult searchResult in (tvControl.ItemsSource as ObservableCollection<SearchResult>)!)
            {
               if (searchResult.FoundItems.Contains(foundItem))
               {
                  return new Tuple<SearchResult, FoundItem?>(searchResult, foundItem);
               }
            }
         }
         else if (selectedItem is SearchResult searchResult)
         {
            return new Tuple<SearchResult, FoundItem?>(searchResult, null);
         }

         return null;
      }
   }
}
