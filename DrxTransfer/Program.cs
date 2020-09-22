using System;
using System.Collections.Generic;
using System.Linq;
using CommandLineParser.Exceptions;
using CommonLibrary;
using CommonLibrary.Exceptions;
using ConfigSettings;

namespace DrxTransfer
{
  class Program
  {
    private static readonly List<string> showUsageCommands = new List<string> { "--help", "/?", "/help" };

    static void Main(string[] args)
    {
      try
      {
        CommandLine.options = ProcessComandLineParameters(args);

        // Если никакие параметры не переданы, либо переданы параметры запроса справки - выходим.
        if (!args.Any() || args.Any(c => showUsageCommands.Contains(c)))
          Environment.Exit(0);

        ChangeConfig.ConfigSettingsPath = CommandLine.options.ConfigSettingsPath;
        AppConfig.Change();

        Client.Initialize();

        CommandLine.RunParsedArgs();

        Client.Cleanup();
        Environment.Exit(0);
      }
      catch (Exception ex)
      {
        var platform = ex as PlatformException;
        Client.Log.Fatal(platform != null ? platform.GetMessageWithDescription() : ex.Message, ex);
        Client.Cleanup();
        Environment.Exit(-1);
      }
    }

    #region Скопировано из DrxUtil.

    private static CommandLineOptions ProcessComandLineParameters(string[] args)
    {
      var options = new CommandLineOptions();
      var commandLineParser = new CommandLineParser.CommandLineParser
      {
        ShowUsageOnEmptyCommandline = true,
        AcceptSlash = true,
        IgnoreCase = true,
      };

      commandLineParser.ExtractArgumentAttributes(options);
      if (!args.Any() || args.Any(c => showUsageCommands.Contains(c)))
      {
        AppConfig.Change();
        LocalizationManager.Instance.AssignCurrentCulture();
        commandLineParser.FillDescFromResource(new CmdResources());
      }

      try
      {
        commandLineParser.ParseCommandLine(args);
      }
      catch (CommandLineException)
      {
        throw new CommandLineException("Incorrect command line parameters");
      }

      return options;
    }

    #endregion
  }
}
