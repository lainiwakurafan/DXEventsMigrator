using DevExpress.Data.Filtering;
using DevExpress.XtraScheduler;

namespace DXEventsMigrator
{
    public static class CriteriaHelper
    {
        public static CriteriaOperator GCRecordNull = new NullOperator("GCRecord");
        public static CriteriaOperator ExceptionEventType = new BinaryOperator("Type", (int)AppointmentType.ChangedOccurrence, BinaryOperatorType.GreaterOrEqual);
        private static readonly CriteriaOperator patternEventType = new BinaryOperator("Type", (int)AppointmentType.Pattern);
        public static CriteriaOperator PatternEvent = new GroupOperator(patternEventType, GCRecordNull);
    }
}
