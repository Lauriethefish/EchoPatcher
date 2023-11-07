using EchoPatcher;
using Serilog;

internal class Program
{


    public static void Main(string[] args)
    {
        SetupLogging();

        try
        {
            Cli(args);
        }
        catch (Exception ex)
        {
            if (ex is MissingResourceException)
            {
                Log.Error(ex.Message);
            }
            else
            {
                Log.Error(ex, "Failed to patch APK");
            }

        }

        Log.CloseAndFlush();
    }

    private static void Cli(string[] args)
    {
        var patcher = new Patcher();

        if (args.Length == 1)
        {
            string fileName = args[0];
            Log.Information("Patching APK at {FileName}", fileName);
            patcher.Patch(fileName);
        }
        else
        {
            Log.Error("Usage: echopatcher <apk path>");
        }
    }

    private static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}