using EchoPatcher;
using Serilog;

internal class Program
{
    const string PackageId = "com.readyatdawn.r15";
    const string VerboseFlag = "-v";

    public static int Main(string[] args)
    {
        string[] argsWithoutFlag = args
            .Where(arg => !arg.Equals(VerboseFlag, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        SetupLogging(argsWithoutFlag.Length != args.Length);

        int exitCode;
        try
        {
            exitCode = Cli(argsWithoutFlag);
        }
        catch (Exception ex)
        {
            if (ex is MissingResourceException)
            {
                Log.Error(ex.Message);
            }
            else if (ex is AdbException adbEx)
            {
                Log.Error(ex.Message);
                Log.Error("ADB stdout: {stdout}", adbEx.Output.StandardOutput);
                Log.Error("ADB stderr: {stdout}", adbEx.Output.ErrorOutput);
            }
            else
            {
                Log.Error(ex, "Failed to patch APK");
            }

            exitCode = 1;
        }

        Log.CloseAndFlush();
        return exitCode;
    }

    private static int Cli(string[] args)
    {
        var patcher = new Patcher();

        if (args.Length == 1)
        {
            string fileName = args[0];
            Log.Information("Patching APK at {FileName}", fileName);
            patcher.Patch(fileName);
            Log.Information("APK patched successfully");
            return 0;
        }
        else if (args.Length == 0)
        {
            return PullPatchAndInstall(patcher);
        }
        else
        {
            Log.Error("Usage: echopatcher [apk path]");
            return 1;
        }
    }

    private static int PullPatchAndInstall(Patcher patcher)
    {
        string temp = Path.Combine(Path.GetTempPath(), "EchoPatcher");
        Directory.CreateDirectory(temp);
        var adb = new AndroidDebugBridge(temp);

        Log.Information("Finding ADB installation");
        adb.PrepareAdbExecutable();

        Log.Information("Downloading APK from Quest");
        string tempApkPath = Path.Combine(temp, "temp.apk");
        if (!adb.DownloadApk(PackageId, tempApkPath))
        {
            Log.Error("EchoVR is not installed on your Quest");
            return 1;
        }

        Log.Information("Patching APK");
        patcher.Patch(tempApkPath);

        adb.UninstallApk(PackageId);

        Log.Information("Installing APK");
        adb.InstallApk(tempApkPath);
        File.Delete(tempApkPath);

        Log.Information("App modded successfully");
        return 0;
    }

    private static void SetupLogging(bool verbose)
    {
        var config = new LoggerConfiguration();
        if (verbose)
        {
            config.MinimumLevel.Verbose();
        }

        Log.Logger = config
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
