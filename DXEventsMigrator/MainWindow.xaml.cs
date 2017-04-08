using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using SpbuEducation.TimeTable;
using SpbuEducation.TimeTable.Appointments;
using SpbuEducation.TimeTable.Appointments.Repositories;
using SpbuEducation.TimeTable.Appointments.StorageInitializers;
using SpbuEducation.TimeTable.BusinessObjects.Events;

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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var watch = Stopwatch.StartNew();

            DoSomeStuff();

            watch.Stop();

            button.Content = $"Elapsed Milliseconds: {watch.ElapsedMilliseconds}";
        }

        private Session _session;

        private const string ConnectionStringName = "testDB";

        private void DoSomeStuff()
        {
            var cursor = new XPCursor(_session, typeof(Event), CriteriaHelper.PatternEvent);
            using (_session = new TimeTableSession { ConnectionString = Configuration.GetConnectionStringByName(ConnectionStringName) })
            {
                foreach (Event patternEvnt in cursor)
                {
                    var patternExceptionEvents = FindExceptions(patternEvnt);

                    ProcessWholePattern(patternEvnt, patternExceptionEvents);
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

                Boom(appts, patternEvnt, eventCreator);
            }
        }

        private void Boom(IEnumerable<TimeEventAppointment> appts, Event e, EventCreator eventCreator)
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

            _session.CommitTransaction();
        }
    }
}
