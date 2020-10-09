using DrxTransfer;
using DrxTransfer.Engine;
using Newtonsoft.Json.Linq;
using Sungero.Domain.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranferSerializers
{
  [Export(typeof(SungeroSerializer))]
  class RegistrationGroupSerializer : SungeroSerializer
  {
    public RegistrationGroupSerializer() : base()
    {
      this.EntityName = "RegistrationGroup";
      this.EntityTypeName = "Sungero.Docflow.IRegistrationGroup";
    }

    public override IEnumerable<IEntity> Filter(IEnumerable<IEntity> entities)
    {
      return entities.Cast<Sungero.Docflow.IRegistrationGroup>().Where(c => c.Status == Sungero.Docflow.RegistrationGroup.Status.Active);
    }

    protected override Dictionary<string, object> Export(IEntity entity)
    {
      base.Export(entity);
      var registrationGroup = entity as Sungero.Docflow.IRegistrationGroup;
      content["Departments"] = registrationGroup.Departments.Select(d => d.Department);
      content["RecipientLinks"] = registrationGroup.RecipientLinks.Select(r => r.Member);
      content["ResponsibleEmployee"] = registrationGroup.ResponsibleEmployee;
      content["Parent"] = registrationGroup.Parent;
      return content;
    }

    public override void Import(Dictionary<string, object> content)
    {
      var entityItem = content["Card"] as JObject;
      var groupName = entityItem.Property("Name").Value.ToString();
      var index = entityItem.Property("Index").Value.ToString();

      var activeRegistrationGroup = Session.Current.GetEntities(this.EntityTypeName).Cast<Sungero.Docflow.IRegistrationGroup>()
         .Where(g => g.Index == index && g.Name == groupName && g.Status == Sungero.Docflow.RegistrationGroup.Status.Active).FirstOrDefault();
      if (activeRegistrationGroup != null)
      {
        throw new System.IO.InvalidDataException(string.Format("Группа регистрации {0} уже существует", groupName));
      }

      var registrationGroup = Session.Current.CreateEntity(this.EntityTypeName) as Sungero.Docflow.IRegistrationGroup;
      Log.Console.Info(string.Format("ИД = {0}. Создание группы регистрации {1}", registrationGroup.Id, groupName));

      registrationGroup.Name = groupName;
      registrationGroup.Index = index;
      registrationGroup.Description = entityItem.Property("Description").Value.ToString();

      Log.Console.Info("Заполнение участников");
      var recipientLinks = SungeroRepository.GetEntities<Sungero.CoreEntities.IRecipient>(content, "RecipientLinks", true, true);
      foreach (var recipient in recipientLinks)
      {
        var recipientLinkItem = registrationGroup.RecipientLinks.AddNew();
        recipientLinkItem.Member = recipient;
      }

      var responsibleEmployeeItem = content["ResponsibleEmployee"] as JObject;
      var responsibleEmployeeName = responsibleEmployeeItem.Property("Name").Value.ToString();
      var responsibleEmployee = Session.Current.GetEntities("Sungero.Company.IEmployee").Cast<Sungero.Company.IEmployee>()
        .FirstOrDefault(e => e.Name == responsibleEmployeeName && e.Status == Sungero.Company.Employee.Status.Active);
      if (responsibleEmployee != null)
        registrationGroup.ResponsibleEmployee = responsibleEmployee;
      else
        throw new System.IO.InvalidDataException(string.Format("Ответственный {0} не найден", responsibleEmployeeName));

      Log.Console.Info("Заполнение подразделений");
      var departments = SungeroRepository.GetEntities<Sungero.Company.IDepartment>(content, "Departments", true, true);
      foreach (var department in departments)
      {
        var departmentItem = registrationGroup.Departments.AddNew();
        departmentItem.Department = department;
      }

      registrationGroup.CanRegisterIncoming = entityItem.Property("CanRegisterIncoming").ToObject<bool?>();
      registrationGroup.CanRegisterOutgoing = entityItem.Property("CanRegisterOutgoing").ToObject<bool?>();
      registrationGroup.CanRegisterInternal = entityItem.Property("CanRegisterInternal").ToObject<bool?>();
      registrationGroup.CanRegisterContractual = entityItem.Property("CanRegisterContractual").ToObject<bool?>();

      registrationGroup.Save();
      Session.Current.SubmitChanges();
    }
  }
}
