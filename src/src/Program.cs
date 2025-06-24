using System;
using System.Windows.Forms;
namespace Clawbyrinth;
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Check if procedural generation mode is requested
        if (args.Length > 0 && args[0] == "--procedural")
        {
            ProceduralGenerationTest.RunProceduralGenerationTest(args);
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }    
}