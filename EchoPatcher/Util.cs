﻿using System.Reflection;
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
                Log.Debug("Using {ResourceName} from local path", name);
                return File.OpenRead(name);
            }
            else
            {
                Log.Debug("Using {ResourceName} from resources", name);
                return Assembly.GetExecutingAssembly().GetManifestResourceStream($"EchoPatcher.Resources.{name}")
                    ?? throw new MissingResourceException(name);
            }
        }
    }
}
