using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using SpbuEducation.TimeTable;
using SpbuEducation.TimeTable.Appointments;
using SpbuEducation.TimeTable.Appointments.Repositories;
using SpbuEducation.TimeTable.Appointments.StorageInitializers;
using SpbuEducation.TimeTable.BusinessObjects.Contingent;
using SpbuEducation.TimeTable.BusinessObjects.Education;
using SpbuEducation.TimeTable.BusinessObjects.Events;
using SpbuEducation.TimeTable.DataProcessing;

namespace DXEventsMigrator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var watch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                DoSomeStuffSeparated();
            });

            watch.Stop();

            button.Content = $"Elapsed Milliseconds: {watch.ElapsedMilliseconds}";
        }

        private Session _session;

        private const string ConnectionStringName = "testDB";
        private IEnumerable<Division> GetDivisions(Session session)
        {
            return new XPCollection<Division>(session);
        }

        private IEnumerable<StudyYear> GetStudyYears(Session session)
        {
            return new XPCollection<StudyYear>(session);
        }

        private XPCursor GetPatternStudyEvents(XPCollection<ContingentUnit> contingentUnits)
        {
            var cursor = new XPCursor(_session, typeof (Event), new GroupOperator(CriteriaHelper.PatternEvent,
                new InOperator("ContingentUnit", contingentUnits)));
            cursor.PageSize = 10;
            return cursor;
        } 

        private void DoSomeStuffSeparated()
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
                        var patternStudyEvents = GetPatternStudyEvents(contingentUnits);
                        foreach (Event patternEvent in patternStudyEvents)
                        {
                            GC.Collect();
                            var patternExceptionEvents = FindExceptions(patternEvent);

                            ProcessWholePattern(patternEvent, patternExceptionEvents);
                        }
                    }
                }
            }
        }

        private void DoSomeStuff()
        {
            using (_session = new TimeTableSession { ConnectionString = Configuration.GetConnectionStringByName(ConnectionStringName) })
            {
                var cursor = new XPCursor(_session, typeof(Event), CriteriaHelper.PatternEvent);
                foreach (Event patternEvnt in cursor)
                {
                    var patternExceptionEvents = FindExceptions(patternEvnt);

                    ProcessWholePattern(patternEvnt, patternExceptionEvents);
                    
                }
                _session.Delete(cursor);
                _session.Save(cursor);
            }
        }

        private void DoSomeStuffWithXPCollection()
        {
            GC.Collect();
            using (
                _session =
                    new TimeTableSession
                    {
                        ConnectionString = Configuration.GetConnectionStringByName(ConnectionStringName)
                    })
            {
                using (
                    XPCollection collection = new XPCollection(_session, typeof (Event),
                        CriteriaHelper.PatternEvent))
                {
                    collection.TopReturnedObjects = 10;
                    // Loop through all the objects. 
                    foreach (Event patternEvnt in collection)
                    {
                        var patternExceptionEvents = FindExceptions(patternEvnt);

                        ProcessWholePattern(patternEvnt, patternExceptionEvents);
                    }
                }
            }
        }

        private XPCollection<Event> FindExceptions(Event patternEvnt)
        {
            var recurrenceId = patternEvnt.RecurrenceInfo.Substring(patternEvnt.RecurrenceInfo.IndexOf("Id="), 41);
            var sameRecurrenceId = new BinaryOperator("RecurrenceInfo", $"%{recurrenceId}%", BinaryOperatorType.Like);

            return new XPCollection<Event>(_session, new GroupOperator(sameRecurrenceId, CriteriaHelper.ExceptionEventType, CriteriaHelper.GCRecordNull));
        }

        private void ProcessWholePattern(Event patternEvnt, IEnumerable<Event> patternExceptionEvents)
        {
            var wholePattern = patternExceptionEvents.ToList();
            wholePattern.Add(patternEvnt);

            using (var appointmentsRepository = new BasicTimeEventAppointmentsRepository(new CommonEventSchedulerStorageInitializer(wholePattern)))
            {
                var appts = appointmentsRepository.GetAppointments();

                var eventCreator = new EventCreator(appointmentsRepository.SchedulerStorage);

                Boom(appts, patternEvnt, eventCreator, patternExceptionEvents);
            }
        }

        private void Boom(IEnumerable<TimeEventAppointment> appts, Event e, EventCreator eventCreator, IEnumerable<Event> patternExceptionEvents)
        {
            _session.BeginTransaction();

            var newSequence = new EventSequence(_session);
            newSequence.Save();

            foreach (var newEvent in appts.Select(a => eventCreator.CreateEvent(a, e, newSequence)))
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
            _session.Delete(patternExceptionEvents);
            _session.Save(patternExceptionEvents);
            e.Delete();
            _session.CommitTransaction();
        }

        private int counter = 0;
    }
}
