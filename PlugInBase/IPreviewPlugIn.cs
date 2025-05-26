using System.Windows.Controls;

namespace PlugInBase;

/// <summary>
/// Plugin for the preview window
/// </summary>
public interface IPreviewPlugIn
{
   /// <summary>some ID that every plugin is individual</summary>
   public Guid ID { get; }
   
   /// <summary>Name and description for the plugin</summary>
   public string Name { get; }
   
   /// <summary>Name and description for the plugin</summary>
   public string Description { get; }
   /// <summary>gives the extenstion for that preview</summary>
   public List<string> Extensions { get; }
   
   /// <summary>Get the preview for that file</summary>
   Control GetPreviewControl(string filePath);

   /// <summary>Goto some page, sheet, cell and select if possible</summary>
   public void GoTo(ParameterHelper param);


}