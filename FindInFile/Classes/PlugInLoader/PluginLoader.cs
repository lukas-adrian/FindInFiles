using PlugInBase;
using System.IO;
using System.Reflection;

namespace FindInFiles.Classes.PlugInLoader
{
   public static class PluginLoader
   {
      public static List<ISearchInFolderPlugIn> LoadPlugins(string directory)
      {
         List<ISearchInFolderPlugIn> plugins = new();

         foreach (string sPlugInDir in Directory.GetDirectories(directory))
         {
            foreach (string file in Directory.GetFiles(sPlugInDir, "*.fif.dll"))
            {
               var loadContext = new PluginLoadContext(file);
               AssemblyName asn = AssemblyName.GetAssemblyName(file);
               Assembly pluginAssembly = loadContext.LoadFromAssemblyName(asn);

               if (pluginAssembly != null)
               {
                  Type[] types = pluginAssembly.GetTypes();

                  bool bCheckNextPlugIn = false;
                  foreach (Type type in types)
                  {
                     if (typeof(ISearchInFolderPlugIn).IsAssignableFrom(type))
                     {
                        ISearchInFolderPlugIn plugin = Activator.CreateInstance(type) as ISearchInFolderPlugIn;
                        plugins.Add(plugin);
                        break;
                     }
                  }
               }
            }
         }




         return plugins;
      }
   }
}
