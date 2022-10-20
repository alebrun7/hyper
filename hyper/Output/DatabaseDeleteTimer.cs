using hyper.config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace hyper.Output
{
    public class DatabaseDeleteTimer
    {
        private Timer aTimer;
        private ProgramConfig programConfig;
        private const int DUETIMEHOURSDEFAULT = 10;
        private const int DUETIMEMINUTESDEFAULT = 2;
        private const int DAYSTOKEEPDEFAULT = 180;
        private const int MAXDAYSTODELETEDEFAULT = 10;
        private int daysToKeep = DAYSTOKEEPDEFAULT;
        private int maxDaysToDelete = MAXDAYSTODELETEDEFAULT;

        public DatabaseDeleteTimer(ProgramConfig programConfig)
        {
            this.programConfig = programConfig;
        }

        public void Start()
        {
            int startWished = programConfig.GetIntValueOrDefault("startDeleteOldEvents", 1);
            if (startWished > 0)
            {
                int dueTimeHours = programConfig.GetIntValueOrDefault("deleteOldEventsHour", DUETIMEHOURSDEFAULT);
                int dueTimeMinutes = programConfig.GetIntValueOrDefault("deleteOldEventsMinutes", DUETIMEMINUTESDEFAULT);

                daysToKeep = programConfig.GetIntValueOrDefault("deleteOldEventsDaysToKeep", DAYSTOKEEPDEFAULT);
                maxDaysToDelete = programConfig.GetIntValueOrDefault("deleteOldEventsMaxDaysToDelete", MAXDAYSTODELETEDEFAULT);

                Common.logger.Info($"DatabaseDeleteTimer scheduled at {dueTimeHours}:{dueTimeMinutes:d2}");

                var dueTime = GetDueTime(dueTimeHours, dueTimeMinutes, DateTime.Now);
                var period = new TimeSpan(1, 0, 0, 0); // 1 day

                Common.logger.Info($"DatabaseDeleteTimer starting in {dueTime.Hours} hours and {dueTime.Minutes} minutes");
                Common.logger.Info($"DatabaseDeleteTimer: keeping {daysToKeep} days of events and deleting for maxmimum {maxDaysToDelete} day(s) each run");

                aTimer = new Timer((e) => DeleteOldEvents(e), null, dueTime, period);
            }
        }

        static public TimeSpan GetDueTime(int dueTimeHours, int dueTimeMinutes, DateTime now)
        {
            DateTime dueDateTime = new DateTime(now.Year, now.Month, now.Day, dueTimeHours, dueTimeMinutes, 0);
            if (dueDateTime < now)
            {
                dueDateTime = dueDateTime.AddDays(1);
            }
            return dueDateTime - now;
        }

        void DeleteOldEvents(object e)
        {
            Common.DeleteOldEvents(daysToKeep, maxDaysToDelete);
        }
    }
}