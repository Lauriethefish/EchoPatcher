using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using Serilog;

namespace EchoPatcher
{
    internal class AndroidDebugBridge
    {
        private readonly string _adbExecutableName = OperatingSystem.IsWindows() ? "adb.exe" : "adb";

        private string? _adbPath;
        private readonly string _platformToolsExtractPath;
        private readonly string _downloadedAdbName;

        public AndroidDebugBridge(string tempPath)
        {
            _platformToolsExtractPath = Path.Combine(tempPath, "platform-tools");
            _downloadedAdbName = Path.Combine(_platformToolsExtractPath, "platform-tools", _adbExecutableName);
        }

        public ProcessOutput RunCommand(string command, bool throwOnFailure = true)
        {
            if (_adbPath == null)
            {
                throw new InvalidOperationException("Attempted to run ADB command before ADB executable could be found");
            }

            Log.Debug("Executing ADB command: adb {Command}", command);
            var output = ProcessUtil.InvokeAndCaptureOutput(_adbPath, command);

            if (output.ExitCode == 0 || output.ExitCode == -1073740940 || !throwOnFailure)
            {
                return output;
            }
            else
            {
                throw new AdbException(command, output);
            }
        }


        public void PrepareAdbExecutable()
        {
            string? existingAdbPath = FindExistingDaemons()
                .Append(_adbExecutableName).FirstOrDefault(IsAdbPathValid);

            // Attempt to extract our built-in ADB executable
            if (existingAdbPath == null)
            {
                if (!Directory.Exists(_platformToolsExtractPath) || !File.Exists(_downloadedAdbName))
                {
                    Log.Debug("Extracting ADB");
                    using var platformTools = Util.GetResource("platform-tools.zip");
                    using var archive = new ZipArchive(platformTools);
                    archive.ExtractToDirectory(_platformToolsExtractPath, true);
                }

                _adbPath = _downloadedAdbName;
            }
            else
            {
                _adbPath = existingAdbPath;
            }
        }

        public void InstallApk(string path)
        {
            string pushPath = $"/data/local/tmp/{Guid.NewGuid()}.apk";

            RunCommand($"push {path} {pushPath}");
            RunCommand($"shell pm install {pushPath}");
            RunCommand($"shell rm {pushPath}");
        }

        public void UninstallApk(string packageId)
        {
            RunCommand($"uninstall {packageId}");
        }

        public bool DownloadApk(string packageId, string destination)
        {
            string rawAppPath = RunCommand($"shell pm path {packageId}", false).StandardOutput;
            if (rawAppPath.Trim().Length == 0)
            {
                return false;
            }

            string appPath = rawAppPath.Remove(0, 8).Replace("\n", "").Replace("'", "").Replace("\r", "");
            RunCommand($"pull {appPath} {destination}");
            return true;
        }

        private bool IsAdbPathValid(string path)
        {
            Log.Debug("Checking if ADB at {Path} is valid", path);
            try
            {
                string output = ProcessUtil.InvokeAndCaptureOutput(path, "version").AllOutput;

                Log.Debug("Valid ADB with version information: {VersionInfo}", output.Trim());
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private IEnumerable<string> FindExistingDaemons()
        {
            return Process.GetProcessesByName("adb")
                .Select(process =>
                {
                    try
                    {
                        return process.MainModule?.FileName;
                    }
                    catch (Win32Exception ex)
                    {
                        Log.Warning(ex, "Could not check process filename");
                        return null;
                    }
                })
                .Where(fullPath => fullPath != null &&
                    Path.GetFileName(fullPath).Equals(_adbExecutableName, StringComparison.OrdinalIgnoreCase))!;
        }
    }
}
