// DUPFINDER_ignore

using Avalonia;

namespace Consolonia.Editor
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithConsoleLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UseConsolonia()
                .UseAutoDetectedConsole()
                //.WithDeveloperTools()
                .LogToException();
        }
    }
}