using System.Linq;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata.Helpers;
using SpbuEducation.TimeTable.XPO;

namespace DXEventsMigrator
{
    public class TimeTableSession : Session
    {
        protected override MemberInfoCollection GetPropertiesListForUpdateInsert(object theObject, bool isUpdate, bool addDelayedReference)
        {
            var result = base.GetPropertiesListForUpdateInsert(theObject, isUpdate, addDelayedReference);
            var membersToRemove = result.Where(memberInfo => memberInfo.HasAttribute(typeof (IgnoreInsertUpdateAttribute))).ToList();

            foreach (var memberInfo in membersToRemove)
            {
                result.Remove(memberInfo);
            }

            return result;
        }
    }
}