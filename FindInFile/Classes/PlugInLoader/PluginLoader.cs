using PlugInBase;
using System.IO;
using System.Reflection;
using Serilog;

namespace FindInFiles.Classes.PlugInLoader
{
   public static class PluginLoader
   {
      private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PluginLoader));
      
      public static (List<ISearchInFolderPlugIn>, List<IPreviewPlugIn>) LoadPlugins(string directory)
      {
         Log.Information("load plugins");
#if DEBUG
         Log.Debug("load plugins from: {dir}", directory);
#endif         
         List<ISearchInFolderPlugIn> pluginsSearch = new();
         List<IPreviewPlugIn> pluginsPreView = new();

         foreach (string sPlugInDir in Directory.GetDirectories(directory))
         {
            foreach (string file in Directory.GetFiles(sPlugInDir, "*.fif.dll"))
            {
#if DEBUG
               Log.Debug("plugin {file} is loading", Path.GetFileName(file));
#endif
               var loadContext = new PluginLoadContext(file);
               AssemblyName asn = AssemblyName.GetAssemblyName(file);
               Assembly pluginAssembly = loadContext.LoadFromAssemblyName(asn);

               if (pluginAssembly != null)
               {
                  Type[] types = pluginAssembly.GetTypes();

                  bool bCheckNextPlugIn = false;
                  foreach (Type type in types)
                  {
#if DEBUG
                     Log.Verbose("type.FullName = {type.FullName}", type.FullName);
                     Log.Verbose("typeof(PlugInBase.ISearchInFolderPlugIn).FullName = {typeof}", typeof(PlugInBase.ISearchInFolderPlugIn).FullName);
                     
                     Type? baseType = type.BaseType;
                     Log.Verbose("baseType?.FullName = {baseType}", baseType?.FullName);

                     var interfaces = type.GetInterfaces();
                     foreach (var iface in interfaces)
                     {
                        Log.Verbose("Implemented interface: = {iface}", iface.FullName);
                     }
                     
                     var methods = type.GetMethods();
                     foreach (var method in methods)
                     {
                        Log.Verbose(
                           "Method: {Name}, Return Type: {ReturnType}, Parameters: {GetParameters().Length}", 
                           method.Name, method.ReturnType, method.GetParameters().Length);
                     }
                     
                     foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                     {
                        Log.Verbose("Loaded assembly assembly.FullName= {assembly.FullName}", assembly.FullName);
                     }
#endif              
                     if (typeof(ISearchInFolderPlugIn).IsAssignableFrom(type))
                     {
#if DEBUG
                        Log.Debug("plugin {file} as ISearchInFolderPlugIn has type {type}", Path.GetFileName(file), type);
#endif
                        ISearchInFolderPlugIn plugin = Activator.CreateInstance(type) as ISearchInFolderPlugIn;
                        pluginsSearch.Add(plugin);
                     }
                     else if (typeof(IPreviewPlugIn).IsAssignableFrom(type))
                     {
#if DEBUG
                        Log.Debug("plugin {file} as IPreviewPlugIn has type {type}", Path.GetFileName(file), type);
#endif
                        IPreviewPlugIn plugin = Activator.CreateInstance(type) as IPreviewPlugIn;
                        pluginsPreView.Add(plugin);
                     }
                  }
               }
            }
         }

         return (pluginsSearch, pluginsPreView);
      }
   }
}
