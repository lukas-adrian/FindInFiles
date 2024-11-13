
namespace FindInFiles;

public class SearchHistory
{
   public HashSet<string> Path { get; set; }
   public HashSet<string> Extension { get; set; }
   public HashSet<string> Text { get; set; }

   public SearchHistory()
   {
      Path = new HashSet<string>();
      Extension = new HashSet<string>();
      Text = new HashSet<string>();
   }
}