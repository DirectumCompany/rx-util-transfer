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
  class DocumentKindSerializer : SungeroSerializer
  {
    public DocumentKindSerializer() : base()
    {
      this.EntityName = "DocumentKind";
      this.EntityTypeName = "Sungero.Docflow.IDocumentKind";
    }

    public override IEnumerable<IEntity> Filter(IEnumerable<IEntity> entities)
    {
      return entities.Cast<Sungero.Docflow.IDocumentKind>().Where(c => c.Status == Sungero.Docflow.DocumentKind.Status.Active);
    }

    public override void Import(Dictionary<string, object> content)
    {
      var entityItem = content["Card"] as JObject;
      var documentFlow = entityItem.Property("DocumentFlow").Value.ToString();
      var kindName = entityItem.Property("Name").Value.ToString();

      var activeDocumendKind = Session.Current.GetEntities(this.EntityTypeName).Cast<Sungero.Docflow.IDocumentKind>()
         .Where(k => k.DocumentFlow.Value.ToString() == documentFlow && k.Name == kindName && k.Status == Sungero.Docflow.DocumentKind.Status.Active).FirstOrDefault();
      if (activeDocumendKind != null)
      {
        throw new System.IO.InvalidDataException(string.Format("Вид документа {0} уже существует", kindName));
      }

      var documentKind = Session.Current.CreateEntity(this.EntityTypeName) as Sungero.Docflow.IDocumentKind;
      Log.Console.Info(string.Format("ИД = {0}. Создание вида документа {1}", documentKind.Id, kindName));

      documentKind.Name = kindName;
      documentKind.DocumentFlow = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentKind.DocumentFlow)).FirstOrDefault(e => e.Value == documentFlow);
      documentKind.Note = entityItem.Property("Note").ToObject<string>();
      documentKind.DeadlineInDays = entityItem.Property("DeadlineInDays").ToObject<int?>();
      documentKind.ShortName = entityItem.Property("ShortName").ToObject<string>();
      documentKind.DeadlineInHours = entityItem.Property("DeadlineInHours").ToObject<int?>();

      var numberingType = entityItem.Property("NumberingType").Value.ToString();
      documentKind.NumberingType = Sungero.Core.Enumeration.GetItems(typeof(Sungero.Docflow.DocumentKind.NumberingType)).FirstOrDefault(e => e.Value == numberingType);

      documentKind.GenerateDocumentName = entityItem.Property("GenerateDocumentName").ToObject<bool?>();
      documentKind.AutoNumbering = entityItem.Property("AutoNumbering").ToObject<bool?>();
      documentKind.ProjectsAccounting = entityItem.Property("ProjectsAccounting").ToObject<bool?>();
      documentKind.GrantRightsToProject = entityItem.Property("GrantRightsToProject").ToObject<bool?>();
      documentKind.IsDefault = entityItem.Property("IsDefault").ToObject<bool?>();

      documentKind.Code = entityItem.Property("Code").ToObject<string>();

      documentKind.AvailableActions.Clear();
      Log.Console.Info("Заполнение действий по отправке");
      var availableActions = SungeroRepository.GetEntities<Sungero.Docflow.IDocumentSendAction>(content, "AvailableActions", true, true);
      foreach (var availableAction in availableActions)
      {
        var availableActionItem = documentKind.AvailableActions.AddNew();
        availableActionItem.Action = availableAction;
      }

      var documentTypeItem = content["DocumentType"] as JObject;
      var documentTypeName = documentTypeItem.Property("Name").Value.ToString();
      var documentType = Session.Current.GetEntities("Sungero.Docflow.IDocumentType").Cast<Sungero.Docflow.IDocumentType>()
        .FirstOrDefault(t => t.DocumentFlow.Value.ToString() == documentFlow && t.Name == documentTypeName);
      if (documentType != null)
        documentKind.DocumentType = documentType;
      else
        throw new System.IO.InvalidDataException(string.Format("Тип документа {0} не найден", documentTypeName));

      documentKind.Save();
      Session.Current.SubmitChanges();
    }

    protected override Dictionary<string, object> Export(IEntity entity)
    {
      base.Export(entity);
      var documentKind = (entity as Sungero.Docflow.IDocumentKind);
      content["AvailableActions"] = documentKind.AvailableActions.Select(a => a.Action);
      content["DocumentType"] = documentKind.DocumentType;
      return content;
    }
  }
}
