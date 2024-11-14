
namespace FindInFiles;

public class SearchHistory
{
   public List<string> Path { get; set; }
   public List<string> Extension { get; set; }
   public List<string> Text { get; set; }

   public SearchHistory()
   {
      Path = new List<string>();
      Extension = new List<string>();
      Text = new List<string>();
   }
}