using System;
using CommonLibrary;
using CommonLibrary.Dependencies;
using CommonLibrary.Exceptions;
using DrxTransfer;
using Sungero.Core;
using Sungero.Deploy.Services;
using Sungero.Domain.Client;
using Sungero.Domain.Client.Deployment;
using Sungero.Domain.ClientBase;
using Sungero.Domain.ClientLinqExpressions;
using Sungero.Domain.Shared;
using Sungero.Domain.Shared.Cache;
using Sungero.Logging;
using Sungero.Metadata.Services;
using Sungero.Presentation;

namespace DrxTransfer
{
  public class Client
  {
    // Логгер
    internal static readonly ILog Log = Logs.GetLogger<Client>();

    /// <summary>
    /// Зарегистрировать клиент.
    /// </summary>
    public static void Initialize()
    {
      DrxTransfer.Log.Console.Info("Регистрация клиента");
      LocalizationManager.Instance.AssignCurrentCulture();

      // Управление кэшами.
      var cacheConfigProvider = new CacheConfigProvider(null, false);
      Dependency.RegisterInstance<ICacheManager>(new CacheManagerImplementation(cacheConfigProvider));

      // Плагины.
      Dependency.RegisterInstance<Sungero.Plugins.IPluginManager>(new Sungero.Plugins.PluginManager(new Sungero.Domain.Client.ClientPluginDiscoverer(), new Sungero.Plugins.PluginSettingsLoader()));

      ServiceContext.Instance.ApplicationExit += (s, a) => { throw a.Exception; };
      if (UserCredentialsManager.IsRegistered)
        return;

      // Перенаправление вывода, чтобы не писать лишнюю информацию на экран.
      var consoleOut = Console.Out;
      try
      {
        Console.SetOut(System.IO.TextWriter.Null);
        Sungero.Domain.Client.SystemInfo.Tenant = CommandLine.options.Tenant;
        AuthenticationHelper.Register(CommandLine.options, false);
        Console.SetOut(consoleOut);
      }
      catch (InvalidSecurityException ex)
      {
        Console.SetOut(consoleOut);
        throw new InvalidSecurityException(ex.IsInternal, new LocalizedString("No access rights to the system - invalid username or password."), ex);
      }
      catch (Exception)
      {
        Console.SetOut(consoleOut);
        throw;
      }

      #region Загрузка модулей.
      Log.Info("Загрузка модулей.");
      DrxTransfer.Log.Console.Info("Загрузка модулей");
      ClientDevelopmentUpdater.Instance.RefreshDevelopment();
      MetadataService.ConfigurationSettingsPaths = new Sungero.Domain.ClientConfigurationSettingsPaths();
      AssemblyResolver.Instance.AddStore<ClientLazyAssembliesStore>(ClientDevelopmentUpdater.Instance.CacheFolder);

      var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
      var developmentDirectory = ClientDevelopmentUpdater.Instance.CacheFolder;

      Dependency.RegisterType<IClientLinqExtensions, ClientLinqExtensionsService>();
      Dependency.RegisterType<IHyperlinkEntityCache, HyperlinkEntityCacheImplementer>();
      Dependency.RegisterType<IHyperlinkDisplayTextCache, HyperlinkDisplayTextCacheImplementer>();

      LoadModules(baseDirectory, "*Client.dll");
      LoadModules(developmentDirectory, null);

      EntityFactory.ConfigureUnityContainer();

      #endregion

      var tenantCulture = TenantInfo.Culture;
      if (!LocalizationManager.Instance.ClientUICulture.Equals(tenantCulture))
      {
        Log.Warn(string.Format("Client culture changed to {0}", tenantCulture));
        LocalizationManager.Instance.SetSystemLanguage(tenantCulture.Name);
        LocalizationManager.Instance.AssignCurrentCulture();
        Cleanup();
        AuthenticationHelper.Register(CommandLine.options, false);
      }
    }

    /// <summary>
    /// Загрузить модули по маске.
    /// </summary>
    /// <param name="folderPath">Путь к папке, из которой будут загружены модули.</param>
    /// <param name="mask">Маска.</param>
    private static void LoadModules(string folderPath, string mask)
    {
      if (string.IsNullOrWhiteSpace(mask))
        ModuleManager.Instance.LoadModules(folderPath);
      else
        ModuleManager.Instance.LoadModules(folderPath, mask);
    }

    /// <summary>
    /// Разрегистрировать клиент.
    /// </summary>
    public static void Cleanup()
    {
      UserCredentialsManager.Unregister();
    }
  }
}
