﻿using hyper.Models;
using LinqToDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ZWave.CommandClasses;

namespace hyper.Database.DAO
{
    internal class EventDAO
    {
        private DataConnection db;

        public EventDAO()
        {
            db = new DataConnection();
            var sp = db.DataProvider.GetSchemaProvider();
            var dbSchema = sp.GetSchema(db);
            if (!dbSchema.Tables.Any(t => t.TableName == "hyper_events"))
            {
                //no required table-create it
                db.CreateTable<Event>();
            }
        }

        public void InsertEvent(Event _event)
        {
            db.Insert(_event);
        }

        public async void InsertEventAsync(Event _event)
        {
            await db.InsertAsync(_event);
        }

        public int DeleteOlderThan(DateTime oldestLimit)
        {
            //deleting ist much faster with a direct statement
            using (var cmd = db.Connection.CreateCommand())
            {
                var param = cmd.CreateParameter();
                param.Value = oldestLimit;
                param.ParameterName = "@dateLimit";
                param.DbType = DbType.DateTime;
                cmd.Parameters.Add(param);
                cmd.CommandText = "delete from hyper_events where added < @dateLimit";
                return cmd.ExecuteNonQuery();
            }
        }

        public void InsertEvents(IEnumerable<Event> _events)
        {
            foreach (var evt in _events)
            {
                db.Insert(evt);
            }
        }

        public async Task InsertEventsAsync(IEnumerable<Event> _events)
        {
            await Task.Run(() =>
            {
                foreach (var evt in _events)
                {
                    db.Insert(evt);
                }
            });
        }

        public Event GetFirst()
        {
            return db.Event.First();
        }

        public IQueryable<Event> GetOldest(int num)
        {
            var oldest = (from e in db.Event
                          orderby e.Added ascending
                          select e).Take(num);
            return oldest;
        }

        public IEnumerable<Event> GetAll()
        {
            //var events =  from e in db.Event
            //              select e;

            string type = typeof(COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT).Name;
            var events = from e in db.Event
                         where e.EventType == type
                         select e;
            return events.ToList();
        }

        public DateTime GetLastEvent(string type, int nodeId)
        {
            /*            var event = from e in db.Event
                                    select e;*/

            var lastEvent = (from e in db.Event
                             where e.EventType == type && e.NodeId == nodeId
                             orderby e.Added descending
                             select e).FirstOrDefault();

            return lastEvent?.Added ?? new DateTime();
        }

        internal List<Event> GetByFilter(EventFilter filter)
        {
            var events = db.Event as IQueryable<Event>;

            if (filter.NodeId.HasValue)
            {
                events = events.Where(e => e.NodeId == filter.NodeId.Value);
            }
            if (filter.Command != null)
            {
                events = events.Where(e => e.EventType == filter.Command.ToUpper());
            }
            events = events.OrderByDescending(e => e.Added);
            if (filter.Count.HasValue)
            {
                events = events.Take(filter.Count.Value);
            }

            return events.AsEnumerable().Reverse().ToList();
        }
    }
}