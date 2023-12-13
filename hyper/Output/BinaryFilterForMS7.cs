using hyper.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace hyper.Output
{
    //Problem: das Multisensor 7 schickt wiederholt binary 1 bei anhaltenden Bewegungen.
    //Diese Wiederholung sollen gefiltert werden.
    //wenn die letzte 1 weniger als 60 Sek. war, ignorieren
    //wenn 0 kommt fängt es von vorne an
    //Es wird eh nur das letzte Event gespeichert
    public class BinaryFilterForMS7
    {
        private const int minTimeBetweenEvents = 59;
        private Dictionary<byte, (DateTime, float)> eventMap;

        public BinaryFilterForMS7(Dictionary<byte, (DateTime, float)> eventMap)
        {
            this.eventMap = eventMap;
        }

        public bool ShouldIgnore(byte srcNodeId, Enums.EventKey eventKey, float eventValue)
        {
            bool shouldIgnore = false;
            if (eventKey == Enums.EventKey.BINARY && eventMap.ContainsKey(srcNodeId))
            {
                var (oldTime, oldValue) = eventMap[srcNodeId];
                double ageInSec = (DateTime.Now - oldTime).TotalSeconds;
                if (ageInSec < minTimeBetweenEvents && oldValue == 1f && eventValue == 1f) {
                    shouldIgnore = true;
                    Common.logger.Info($"Repeated Binary event ignored for node {srcNodeId}");
                }
            }
            StoreEvent(srcNodeId, eventKey, eventValue);
            return shouldIgnore;
        }

        internal void StoreEvent(byte srcNodeId, Enums.EventKey eventKey, float eventValue)
        {
            if (eventKey == Enums.EventKey.BINARY)
            {
                eventMap[srcNodeId] = (DateTime.Now, eventValue);
            }
        }
    }
}
