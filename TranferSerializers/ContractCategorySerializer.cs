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
  class ContractCategorySerializer : SungeroSerializer
  {
    public ContractCategorySerializer() : base()
    {
      this.EntityName = "ContractCategory";
      this.EntityTypeName = "Sungero.Contracts.IContractCategory";
    }

    public override IEnumerable<IEntity> Filter(IEnumerable<IEntity> entities)
    {
      return entities.Cast<Sungero.Contracts.IContractCategory>().Where(c => c.Status == Sungero.Contracts.ContractCategory.Status.Active);
    }

    public override void Import(Dictionary<string, object> content)
    {
      var entityItem = content["Card"] as JObject;
      var categoryName = entityItem.Property("Name").Value.ToString();

      var activeContractCategory = Session.Current.GetEntities(this.EntityTypeName).Cast<Sungero.Contracts.IContractCategory>()
         .Where(c => c.Name == categoryName && c.Status == Sungero.Contracts.ContractCategory.Status.Active).FirstOrDefault();
      if (activeContractCategory != null)
      {
        throw new System.IO.InvalidDataException(string.Format("Категория договора {0} уже существует", categoryName));
      }

      var сontractCategory = Session.Current.CreateEntity(this.EntityTypeName) as Sungero.Contracts.IContractCategory;
      Log.Console.Info(string.Format("ИД = {0}. Создание категории договора {1}", сontractCategory.Id, categoryName));
      сontractCategory.Name = categoryName;
      сontractCategory.Note = entityItem.Property("Note").ToObject<string>();

      var documentKinds = SungeroRepository.GetEntities<Sungero.Docflow.IDocumentKind>(content, "DocumentKinds", false, true);
      Log.Console.Info("Заполнение видов документов");
      foreach (var documentKind in documentKinds)
      {
        var documentKindItem = сontractCategory.DocumentKinds.AddNew();
        documentKindItem.DocumentKind = documentKind;
      }

      сontractCategory.Save();
      Session.Current.SubmitChanges();
    }

    protected override Dictionary<string, object> Export(IEntity entity)
    {
      base.Export(entity);
      var contractCategory = entity as Sungero.Contracts.IContractCategory;
      content["DocumentKinds"] = contractCategory.DocumentKinds.Select(k => k.DocumentKind);
      return content;
    }
  }
}
