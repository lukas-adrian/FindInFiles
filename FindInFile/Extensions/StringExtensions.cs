using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FindInFiles.Extensions
{
    public static class StringExtensions
   {
      public static bool IsNumeric(this string text)
      {
         if (string.IsNullOrEmpty(text))
            return false;

         if (text.All(char.IsDigit))
            return true;

         if (text.Length == 1 && text == ".")
            return true;

         // Additional checks if needed, such as allowing only one decimal point
         if (text.Contains(".") && text.Split('.').Length > 2)
            return false;

         return false;
      }
   }
}
