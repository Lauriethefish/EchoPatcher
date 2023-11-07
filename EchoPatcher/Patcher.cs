using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using QuestPatcher.Axml;
using QuestPatcher.Zip;
using Serilog;

namespace EchoPatcher
{
    internal class Patcher
    {
        private const string LibsPath = "lib/arm64-v8a";

        private const string ProxyName = "libr15.so";

        private const string ProxyPath = $"{LibsPath}/{ProxyName}";
        private const string OriginalPath = $"{LibsPath}/libr15-original.so";
        private const string TagPath = "modded.json";
        private const string ManifestPath = "AndroidManifest.xml";
        private const CompressionLevel CompressLevel = CompressionLevel.Optimal;

        public void Patch(string path)
        {
            using var replacementStream = Util.GetResource(ProxyName);

            using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
            using var apk = ApkZip.Open(stream);

            if (apk.ContainsFile(TagPath))
            {
                Log.Warning("APK already has a tag, so is already modded. " +
                    "Our modloader will still be installed, but this may result in a crash and/or the existing modloader being overwritten");
            }

            Log.Information("Adding proxy main to the APK");
            AddProxy(apk, replacementStream);

            Log.Information("Patching manifest (allow debugging + MANAGE_EXTERNAL_STORAGE)");
            PatchManifest(apk);

            Log.Information("Adding tag");
            AddTag(apk);

            Log.Information("Signing the APK");
            // Disposing the APK happens automatically, and signs the APK.
        }

        private void AddProxy(ApkZip apk, Stream replacementStream)
        {

            if (!apk.ContainsFile(OriginalPath))
            {
                using var originalMs = new MemoryStream();

                using (var originalReader = apk.OpenReader(ProxyPath))
                {
                    originalReader.CopyTo(originalMs);
                }

                originalMs.Position = 0;
                apk.AddFile(OriginalPath, originalMs, CompressLevel);
            }


            apk.AddFile(ProxyPath, replacementStream, CompressLevel);
        }

        private void AddTag(ApkZip apk)
        {
            var assemblyName = Assembly.GetEntryAssembly()!.GetName(); // Definitely not null, called from managed code.
            var version = assemblyName.Version;
            if (version == null)
            {
                Log.Warning("Failed to add tag - could not fetch assembly version");
                return;
            }
            string semVersion = $"{version.Major}.{version.Minor}.{version.Build}";

            var tag = new ModdedTag(assemblyName.Name!, semVersion, "libr15loader", null);

            using var tagMs = new MemoryStream();
            JsonSerializer.Serialize(tagMs, tag, SourceGenerationContext.Default.ModdedTag);

            tagMs.Position = 0;
            apk.AddFile(TagPath, tagMs, CompressLevel);
        }

        private void PatchManifest(ApkZip apk)
        {
            using var manifestMs = new MemoryStream();
            using (var manifestReader = apk.OpenReader(ManifestPath))
            {
                manifestReader.CopyTo(manifestMs);
            }

            manifestMs.Position = 0;
            var rootElement = AxmlLoader.LoadDocument(manifestMs);

            bool modified = ModifyManifest(rootElement);
            if (modified)
            {
                manifestMs.SetLength(0);
                AxmlSaver.SaveDocument(manifestMs, rootElement);
                manifestMs.Position = 0;

                apk.AddFile(ManifestPath, manifestMs, CompressLevel);
            }
        }

        private bool ModifyManifest(AxmlElement rootElement)
        {
            const int NameAttributeResourceId = 16842755;
            const int DebuggableAttributeResourceId = 16842767;
            var AndroidNamespaceUri = new Uri("http://schemas.android.com/apk/res/android");

            var addingPerms = new List<string>
            {
                "READ_EXTERNAL_STORAGE",
                "WRITE_EXTERNAL_STORAGE",
                "MANAGE_EXTERNAL_STORAGE"
            };

            var existingPerms = rootElement.Children
                .Where(child => child.Name == "uses-permission")
                .Select(child => child.Attributes.Single(attr => attr.Name == "name" && attr.Namespace == AndroidNamespaceUri).Value)
                .ToHashSet();

            bool modified = false;

            foreach (string perm in addingPerms)
            {
                if (existingPerms.Contains(perm))
                {
                    continue;
                }

                var permElement = new AxmlElement("uses-permission");
                permElement.Attributes.Add(new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId, perm));
                rootElement.Children.Add(permElement);

                modified = true;
            }

            var appElement = rootElement.Children
                .Single(child => child.Name == "application");
            var debugAttribute = appElement.Attributes.FirstOrDefault(attr => attr.Name == "debuggable" && attr.Namespace == AndroidNamespaceUri);

            if (debugAttribute != null)
            {
                if ((bool) debugAttribute.Value != true)
                {
                    debugAttribute.Value = true;
                    modified = true;
                }
            }
            else
            {
                appElement.Attributes.Add(new AxmlAttribute("debuggable", AndroidNamespaceUri, DebuggableAttributeResourceId, true));
            }

            return modified;
        }
    }
}
