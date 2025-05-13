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
                     /*Console.WriteLine(type.FullName);  // Log the type of 'type' being checked
                     Console.WriteLine(typeof(PlugInBase.ISearchInFolderPlugIn).FullName);  // Log the FullName of the interface
                     
                     var baseType = type.BaseType;
                     Console.WriteLine($"Base type: {baseType?.FullName}");

                     var interfaces = type.GetInterfaces();
                     foreach (var iface in interfaces)
                     {
                        Console.WriteLine($"Implemented interface: {iface.FullName}");
                     }
                     
                     var methods = type.GetMethods();
                     foreach (var method in methods)
                     {
                        Console.WriteLine($"Method: {method.Name}, Return Type: {method.ReturnType}, Parameters: {method.GetParameters().Length}");
                     }
                     
                     foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                     {
                        Console.WriteLine($"Loaded assembly: {assembly.FullName}");
                     }*/
                     
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
