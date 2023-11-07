using System.Text.Json.Serialization;

namespace EchoPatcher
{
    internal class ModdedTag
    {
        public string PatcherName { get; set; }

        public string? PatcherVersion { get; set; }

        public string ModloaderName { get; set; }

        public string? ModloaderVersion { get; set; }

        [JsonConstructor]
        public ModdedTag(string patcherName, string? patcherVersion, string modloaderName, string? modloaderVersion)
        {
            PatcherName = patcherName;
            PatcherVersion = patcherVersion;
            ModloaderName = modloaderName;
            ModloaderVersion = modloaderVersion;
        }
    }
}
