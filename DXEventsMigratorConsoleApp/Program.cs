using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.XtraScheduler;
using DXEventsMigrator;
using SpbuEducation.TimeTable;
using SpbuEducation.TimeTable.Appointments;
using SpbuEducation.TimeTable.Appointments.Repositories;
using SpbuEducation.TimeTable.Appointments.StorageInitializers;
using SpbuEducation.TimeTable.BusinessObjects.Contingent;
using SpbuEducation.TimeTable.BusinessObjects.Education;
using SpbuEducation.TimeTable.BusinessObjects.Events;

namespace DXEventsMigratorConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var watch = Stopwatch.StartNew();

            
            MigrateSeparated();

            watch.Stop();

            Console.WriteLine($"Elapsed Milliseconds: {watch.ElapsedMilliseconds}");
        }

        private static Session _session;

        private const string ConnectionStringName = "testDB";
        private static IEnumerable<Division> GetDivisions(Session session)
        {
            return new XPCollection<Division>(session);
        }

        private static IEnumerable<StudyYear> GetStudyYears(Session session)
        {
            return new XPCollection<StudyYear>(session);
        }

        private static XPCollection<Event> GetPatternsAndExceptions(XPCollection<ContingentUnit> contingentUnits)
        {
            var cursor = new XPCollection<Event>(_session,
                new GroupOperator(
                    CriteriaOperator.Or(CriteriaHelper.PatternEvent, CriteriaHelper.ExceptionEventType),
                    new InOperator("ContingentUnit", contingentUnits)));
            return cursor;
        }

        private static void MigrateSeparated()
        {
            using (
                _session =
                    new TimeTableSession
                    {
                        ConnectionString = Configuration.GetConnectionStringByName(ConnectionStringName)
                    })
            {
                var divisions = GetDivisions(_session);
                foreach (var division in divisions)
                {
                    foreach (var studyYear in GetStudyYears(_session))
                    {
                        var contingentUnitsCriteria = CriteriaOperator.And(
                            new BinaryOperator("Division", division),
                            new BinaryOperator("CurrentStudyYear", studyYear));
                        var contingentUnits = new XPCollection<ContingentUnit>(_session, contingentUnitsCriteria);
                        var patternsAndExceptions = GetPatternsAndExceptions(contingentUnits);
                        using (var appointmentsRepository = new BasicTimeEventAppointmentsRepository(new CommonEventSchedulerStorageInitializer(patternsAndExceptions), new DateTime(2000,1,1), new DateTime(2020, 1,1)))
                        {
                            var appts = appointmentsRepository.GetAppointments();

                            var eventCreator = new NormalEventCreator(appointmentsRepository.SchedulerStorage);

                            Boom(appts, eventCreator, appointmentsRepository.SchedulerStorage);
                        }
                    }
                }
            }
        }

        private static XPCollection<Event> FindExceptions(Event patternEvnt)
        {
            var recurrenceId = patternEvnt.RecurrenceInfo.Substring(patternEvnt.RecurrenceInfo.IndexOf("Id="), 41);
            var sameRecurrenceId = new BinaryOperator("RecurrenceInfo", $"%{recurrenceId}%", BinaryOperatorType.Like);

            return new XPCollection<Event>(_session, new GroupOperator(sameRecurrenceId, CriteriaHelper.ExceptionEventType, CriteriaHelper.GCRecordNull));
        }

        private static void Boom(IEnumerable<TimeEventAppointment> appts, NormalEventCreator normalEventCreator, SchedulerStorage storage)
        {
            _session.BeginTransaction();

            var timeEventAppointments = appts as TimeEventAppointment[] ?? appts.ToArray();
            foreach (var appt in timeEventAppointments)
            {
                Event e;
                if (appt.Type == AppointmentType.Occurrence)
                {
                    e = appt.RecurrencePattern.GetSourceObject(storage) as Event;
                }
                else
                {
                    e = appt.GetSourceObject(storage) as Event;
                    if (e == null)
                    {
                        Console.WriteLine($"Не найден исходный объект события. Тип: {appt.Type}");
                        continue;
                    }
                }
                var newEvent = normalEventCreator.CreateEvent(appt, e);
                foreach (var el in e.EventLocations)
                {
                    var newEl = new EventLocation(_session)
                    {
                        Event = newEvent,
                        Location = el.Location
                    };
                    newEl.Save();

                    foreach (var newLe in el.Educators.Select(le => new LocationEducator(_session)
                    {
                        Educator = le.Educator,
                        EducatorEmployment = le.EducatorEmployment,
                        EventLocation = newEl
                    }))
                    {
                        newLe.Save();
                    }
                }
            }
            _session.CommitTransaction();
            if (timeEventAppointments.Length > 0)
            {
                Console.WriteLine($"{timeEventAppointments.Length} Normals added");
            }
            GC.Collect();
        }

        private static void Boom(IEnumerable<TimeEventAppointment> appts, Event e, NormalEventCreator normalEventCreator, IEnumerable<Event> patternExceptionEvents)
        {
            _session.BeginTransaction();

            var timeEventAppointments = appts as TimeEventAppointment[] ?? appts.ToArray();
            foreach (var newEvent in timeEventAppointments.Select(a => normalEventCreator.CreateEvent(a, e)))
            {
                foreach (var el in e.EventLocations)
                {
                    var newEl = new EventLocation(_session)
                    {
                        Event = newEvent,
                        Location = el.Location
                    };
                    newEl.Save();

                    foreach (var newLe in el.Educators.Select(le => new LocationEducator(_session)
                    {
                        Educator = le.Educator,
                        EducatorEmployment = le.EducatorEmployment,
                        EventLocation = newEl
                    }))
                    {
                        newLe.Save();
                    }
                }
            }
            Console.WriteLine($"{timeEventAppointments.Length} Normals added");
            _session.Delete(patternExceptionEvents);
            _session.Save(patternExceptionEvents);
            Console.WriteLine($"{patternExceptionEvents.Count()} Exceptions deleted");
            _session.Delete(e);
            Console.WriteLine("1 Pattern deleted");
            _session.CommitTransaction();
        }

        private int counter = 0;
    }
}
