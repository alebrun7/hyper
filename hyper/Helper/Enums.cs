﻿namespace hyper.Helper
{
    public static class Enums
    {
        public enum EventKey
        {
            BINARY,
            UNKNOWN,
            WAKEUP,
            TEMPERATURE,
            HUMIDITY,
            POWER,
            OPEN,
            CLOSE,
            MOTION,
            IDLE,
            TAMPER,
            BATTERY,
            IMPACT,
            STATE_ON,
            STATE_CLOSED,
            BASIC,
            ILLUMINANCE,
            ULTRAVIOLET,
            THERMOSTAT_SETPOINT,
            THERMOSTAT_OPERATING_STATE,
            THERMOSTAT_MODE,
            CHANNEL_1_STATE,
            CHANNEL_2_STATE,
            SCENE,
        }

        //public static EventKey GetEventTypeByMultilevelType(byte eventValue)
        //{
        //    return eventValue switch
        //    {
        //        0x01 => EventKey.TEMPERATURE,
        //        0x04 => EventKey.POWER,
        //        0x05 => EventKey.HUMIDITY,
        //        _ => EventKey.UNKNOWN,
        //    };
        //}
    }
}