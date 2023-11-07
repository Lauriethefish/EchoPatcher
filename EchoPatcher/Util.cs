using System.Reflection;
using Serilog;

namespace EchoPatcher
{
    internal static class Util
    {
        internal static Stream GetResource(string name)
        {
            // Use an override file if exists
            if (File.Exists(name))
            {
                Log.Debug("Using {ResourceName} from local path");
                return File.OpenRead(name);
            }
            else
            {
                Log.Debug("Using {ResourceName} from resources");
                return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                    ?? throw new MissingResourceException(name);
            }
        }
    }
}
