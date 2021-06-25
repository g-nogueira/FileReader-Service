using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkService.Service.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IOTLinkService.Service.Loaders
{
    internal class AssemblyLoader
    {
        internal static bool LoadAssemblyDLL(ref AddonInfo addonInfo)
        {
            try
            {
                string filename = Path.Combine(addonInfo.AddonPath, addonInfo.AddonFile);

                if (File.Exists(filename))
                {
                    byte[] bytes = File.ReadAllBytes(filename);
                    Assembly asb = Assembly.Load(bytes);

                    // Load AddonScript from the assembly.
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

                    IEnumerable<Type> scriptTypeList = asb.GetExportedTypes().Where(w => w.IsSubclassOf(typeof(ServiceAddon)));

                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomain_ReflectionOnlyAssemblyResolve;
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

                    Type scriptType = null;
                    if (scriptTypeList.Count() > 0)
                        scriptType = scriptTypeList.FirstOrDefault();

                    if (scriptType != null)
                    {
                        LoggerHelper.Debug("Found ServiceAddon!");
                        addonInfo.ServiceAddon = (ServiceAddon)Activator.CreateInstance(scriptType);
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Error("Unhandled Exception: {0}", e.ToString());
            }

            return false;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(typeof(ServiceMain).Assembly.GetName().Name))
            {
                return Assembly.GetExecutingAssembly();
            }
            return null;
        }

        private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly asb = AppDomain.CurrentDomain.GetAssemblies().Where(w => w.FullName == args.Name).FirstOrDefault();
            return asb;
        }
    }
}
