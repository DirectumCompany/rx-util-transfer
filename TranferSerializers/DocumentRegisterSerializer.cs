using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using DrxTransfer;
using DrxTransfer.Engine;
using Newtonsoft.Json.Linq;
using Sungero.Domain.Shared;

namespace TranferSerializers
{
  [Export(typeof(SungeroSerializer))]
  class DocumentRegisterSerializer : SungeroSerializer
  {
    public DocumentRegisterSerializer() : base()
    {
      this.EntityName = "DocumentRegister";
      this.EntityTypeName = "Sungero.Docflow.IDocumentRegister";
    }

    public override IEnumerable<IEntity> Filter(IEnumerable<IEntity> entities)
    {
      return entities.Cast<Sungero.Docflow.IDocumentRegister>().Where(c => c.Status == Sungero.Docflow.DocumentRegister.Status.Active);
    }

    public override void Import(Dictionary<string, object> content)
    {
      var entityItem = content["Card"] as JObject;
      var registerName = entityItem.Property("Name").Value.ToString();
      var documentFlow = entityItem.Property("DocumentFlow").Value.ToString();

      var activeDocumentRegister = Session.Current.GetEntities(this.EntityTypeName).Cast<Sungero.Docflow.IDocumentRegister>()
         .Where(k => k.DocumentFlow.Value.ToString() == documentFlow && k.Name == registerName && k.Status == Sungero.Docflow.DocumentRegister.Status.Active).FirstOrDefault();
      if (activeDocumentRegister != null)
      {
        throw new System.IO.InvalidDataException(string.Format("Журнал регистрации {0} уже существует", registerName));
      }

      var documentRegister = Session.Current.CreateEntity(this.EntityTypeName) as Sungero.Docflow.IDocumentRegister;
      Log.Console.Info(string.Format("ИД = {0}. Создание журнала регистрации {1}", documentRegister.Id, registerName));

      documentRegister.Name = registerName;
      documentRegister.DocumentFlow = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentRegister.DocumentFlow)).FirstOrDefault(e => e.Value == documentFlow);
      documentRegister.Index = entityItem.Property("Index").Value.ToString();
      documentRegister.NumberOfDigitsInNumber = entityItem.Property("NumberOfDigitsInNumber").ToObject<int?>();

      var registerType = entityItem.Property("RegisterType").Value.ToString();
      documentRegister.RegisterType = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentRegister.RegisterType)).FirstOrDefault(e => e.Value == registerType);

      var numberingPeriod = entityItem.Property("NumberingPeriod").Value.ToString();
      documentRegister.NumberingPeriod = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentRegister.NumberingPeriod)).FirstOrDefault(e => e.Value == numberingPeriod);

      var numberingSection = entityItem.Property("NumberingSection").Value.ToString();
      documentRegister.NumberingSection = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentRegister.NumberingSection)).FirstOrDefault(e => e.Value == numberingSection);

      documentRegister.NumberFormatItems.Clear();
      var numberFormatItems = entityItem.Property("NumberFormatItems").Value;
      foreach (var numberFormatItemJToken in numberFormatItems)
      {
        var newNumberFormatItem = documentRegister.NumberFormatItems.AddNew();
        newNumberFormatItem.Number = numberFormatItemJToken["Number"].ToObject<int?>();
        newNumberFormatItem.Separator = numberFormatItemJToken["Separator"].ToObject<string>();
        var element = numberFormatItemJToken["Element"].ToObject<string>();
        newNumberFormatItem.Element = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentRegisterNumberFormatItems.Element)).FirstOrDefault(e => e.Value == element);
      }

      var registrationGroupItem = content["RegistrationGroup"] as JObject;
      if (registrationGroupItem != null)
      {
        var registrationGroupName = registrationGroupItem.Property("Name").Value.ToString();
        var registrationGroup = Session.Current.GetEntities("Sungero.Docflow.IRegistrationGroup").Cast<Sungero.Docflow.IRegistrationGroup>()
          .FirstOrDefault(g => g.Name == registrationGroupName && g.Status == Sungero.Docflow.RegistrationGroup.Status.Active);
        if (registrationGroup != null)
          documentRegister.RegistrationGroup = registrationGroup;
        else
          throw new System.IO.InvalidDataException(string.Format("Группа регистрации {0} не найдена", registrationGroupName));
      }

      documentRegister.Save();
      Session.Current.SubmitChanges();
    }

    protected override Dictionary<string, object> Export(IEntity entity)
    {
      base.Export(entity);
      content["RegistrationGroup"] = (entity as Sungero.Docflow.IDocumentRegister).RegistrationGroup;
      return content;
    }
  }
}
