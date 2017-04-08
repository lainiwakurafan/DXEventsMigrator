using DevExpress.XtraScheduler;
using SpbuEducation.TimeTable.Appointments;
using SpbuEducation.TimeTable.BusinessObjects.Events;

namespace DXEventsMigrator
{
    public class EventCreator
    {
        private readonly SchedulerStorage storage;
        public EventCreator(SchedulerStorage storage)
        {
            this.storage = storage;
        }

        public Event CreateEvent(TimeEventAppointment appt, Event patternEvnt, EventSequence sequence)
        {
            var e = new Event(patternEvnt.Session)
            {
                Type = (int) AppointmentType.Normal,
                Sequence = sequence
            };


            var sourceEvent = appt.GetSourceObject(storage) as Event;
            if (sourceEvent != null) patternEvnt = sourceEvent;

            e.IsAllDayEvent = patternEvnt.IsAllDayEvent;
            e.Start = appt.Start;
            e.End = appt.End;
            e.EducatorAssignment = patternEvnt.EducatorAssignment;
            e.LabelId = patternEvnt.LabelId;
            e.ContingentUnit = patternEvnt.ContingentUnit;
            e.Subject = patternEvnt.Subject;
            e.LocationsDisplayText = patternEvnt.LocationsDisplayText;
            e.EducatorsDisplayText = patternEvnt.EducatorsDisplayText;
            e.InternalDescription = patternEvnt.InternalDescription;

            e.TermKind = patternEvnt.TermKind;
            e.Kind = patternEvnt.Kind;
            //e.ObjectType = evnt.ObjectType;
            e.StudyEventsTimeTableKind = patternEvnt.StudyEventsTimeTableKind;
            //e.RecurrenceInfo = evnt.RecurrenceInfo;
            e.ResourceIds = patternEvnt.ResourceIds;
            e.SubjectEnglish = patternEvnt.SubjectEnglish;
            e.LocationsDisplayTextEnglish = patternEvnt.LocationsDisplayTextEnglish;
            e.EducatorsDisplayTextEnglish = patternEvnt.EducatorsDisplayTextEnglish;
            e.EducatorsShortDisplayText = patternEvnt.EducatorsShortDisplayText;
            e.EducatorsShortDisplayTextEnglish = patternEvnt.EducatorsShortDisplayTextEnglish;
            e.PatternEvent = patternEvnt;
            e.Subkind = patternEvnt.Subkind;
            e.TimeTable = patternEvnt.TimeTable;
            e.MasterEvent = patternEvnt.MasterEvent;
            e.ResponsiblePersonContacts = patternEvnt.ResponsiblePersonContacts;
            e.IsComposite = patternEvnt.IsComposite;
            e.DescriptionLong = patternEvnt.DescriptionLong;
            e.XtracurImageAttachment = patternEvnt.XtracurImageAttachment;
            e.XtracurAddress = patternEvnt.XtracurAddress;
            //e.Id = evnt.Id;
            e.WebAvailability = patternEvnt.WebAvailability;
            e.IsCancelled = patternEvnt.IsCancelled;
            //e.IsImported = evnt.IsImported;
            //e.ImportOid = evnt.ImportOid;
            //e.ImportSession = evnt.ImportSession;
            e.LastChangeUser = patternEvnt.LastChangeUser;
            e.LastChangeDateTime = patternEvnt.LastChangeDateTime;

            e.EducatorsWereReassigned = patternEvnt.EducatorsWereReassigned;
            e.LocationsWereChanged = patternEvnt.LocationsWereChanged;
            e.AssignedDateTime = patternEvnt.AssignedDateTime;
            e.WasScheduled = patternEvnt.WasScheduled;
            e.TimeWasChanged = patternEvnt.TimeWasChanged;
            e.DontShowEndTimeOnWeb = patternEvnt.DontShowEndTimeOnWeb;

            return e;
        }
    }
}
