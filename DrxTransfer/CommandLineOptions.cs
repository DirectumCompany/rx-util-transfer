using System.Resources;
using CommandLineParser;
using CommandLineParser.Arguments;
using Sungero.Domain.Client;

namespace DrxTransfer
{
  #region Скопировано из DrxUtil.

  public sealed class CommandLineOptions : CommandLineOptionsBase
  {
    #region Поля и свойства

    /// <summary>
    /// Экспорт сущностей.
    /// </summary>
    [ValueArgument(typeof(string), 'x', "Export", FullDescription = "Export", Optional = true)]
    public string Export { get; set; }

    /// <summary>
    /// Импорт сущностей.
    /// </summary>
    [ValueArgument(typeof(string), 'i', "Import", FullDescription = "Import", Optional = true)]
    public string Import { get; set; }

    /// <summary>
    /// Импорт сущностей.
    /// </summary>
    [ValueArgument(typeof(string), 'j', "EntityType", FullDescription = "EntityType", Optional = true)]
    public string EntityType { get; set; }

    /// <summary>
    /// Запустить в указанном тенанте.
    /// </summary>
    [ValueArgument(typeof(string), 'z', "Tenant", FullDescription = "Tenant", Optional = true)]
    public new string Tenant { get; set; }

    /// <summary>
    /// Количество попыток выполнения инициализации.
    /// </summary>
    [ValueArgument(typeof(int), 'u', "Attempts", DefaultValue = 1, FullDescription = "Attempts", Optional = true)]
    public int Attempts { get; set; }

    /// <summary>
    /// MultiInstance не поддерживается, переопределяем.
    /// </summary>
    public new bool MultiInstance { get; set; }

    /// <summary>
    /// ForcePromptForCredentials не поддерживается, переопределяем.
    /// </summary>
    public new bool ForcePromptForCredentials { get; set; }

    /// <summary>
    /// SystemUsed не поддерживается, переопределяем.
    /// </summary>
    public new string SystemUsed { get; set; }

    /// <summary>
    /// Hyperlink не поддерживается, переопределяем.
    /// </summary>
    public new string Hyperlink { get; set; }

    /// <summary>
    /// EnableExtendedAdministrativeFunctions не поддерживается, переопределяем.
    /// </summary>
    public new bool EnableExtendedAdministrativeFunctions { get; set; }

    #endregion
  }

  public class CmdResources : IResource
  {
    public ResourceManager ResourceManager
    {
      get
      {        
        return DrxTransfer.Properties.Resources.ResourceManager;
      }
    }
  }

  #endregion
}
