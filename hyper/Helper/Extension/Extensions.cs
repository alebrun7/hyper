using System;
using System.Linq;
using Utils;
using ZWave.CommandClasses;

namespace hyper.Helper.Extension
{
    public static class Extensions
    {
        public static bool GetKeyValue(this object obj, out Enums.EventKey eventType, out float floatVal)
        {
            //if obj is COMMAND_CLASS_SENSOR_MULTILEVEL_V11.SENSOR_MULTILEVEL_REPORT
            switch (obj)
            {
                case COMMAND_CLASS_SENSOR_MULTILEVEL_V11.SENSOR_MULTILEVEL_REPORT multiReport:
                    {
                        var size = multiReport.properties1.size;
                        var precision = multiReport.properties1.precision;
                        var type = multiReport.sensorType;

                        eventType = type switch
                        {
                            0x01 => Enums.EventKey.TEMPERATURE,
                            0x03 => Enums.EventKey.ILLUMINANCE,
                            0x04 => Enums.EventKey.POWER,
                            0x05 => Enums.EventKey.HUMIDITY,
                            0x1B => Enums.EventKey.ULTRAVIOLET,
                            _ => Enums.EventKey.UNKNOWN,
                        };

                        if (size == 1)
                        {
                            var byteVal = multiReport.sensorValue[0];
                            floatVal = (float)byteVal;
                        }
                        else if (size == 2)
                        {
                            var shortVal = BitConverter.ToInt16(multiReport.sensorValue.Reverse().ToArray(), 0);
                            floatVal = (float)shortVal;
                        }
                        else if (size == 4)
                        {
                            var intVal = BitConverter.ToInt32(multiReport.sensorValue.Reverse().ToArray(), 0);
                            floatVal = (float)intVal;
                        }
                        else
                        {
                            Console.WriteLine("unknown size: {0}", size);
                            goto default;
                            //floatVal = -1;
                            //return false;
                        }
                        floatVal /= MathF.Pow(10f, precision);
                        return true;
                    }
                case COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT notificationReport:
                    {
                        var type = (NotificationType)notificationReport.notificationType;
                        // 0x07 Home Security
                        if (type == NotificationType.HomeSecurity) //0x07)
                        {
                            var mevent = (HomeSecurityEvent)notificationReport.mevent;
                            eventType = mevent switch
                            {
                                HomeSecurityEvent.StateIdle => Enums.EventKey.MOTION, // 0x00
                                HomeSecurityEvent.TamperingProductCoverRemoved => Enums.EventKey.TAMPER, // 0x03
                                HomeSecurityEvent.TamperingInvalidCode => Enums.EventKey.TAMPER, // 0x04
                                HomeSecurityEvent.TamperingProductMoved => Enums.EventKey.TAMPER, //  0x09
                                HomeSecurityEvent.MotionDetectionLocationProvided => Enums.EventKey.MOTION, // 0x07
                                HomeSecurityEvent.MotionDetection => Enums.EventKey.MOTION, // 0x08
                                HomeSecurityEvent.ImpactDetected => Enums.EventKey.IMPACT, // 0x0a
                                _ => Enums.EventKey.UNKNOWN,
                            };
                            floatVal = mevent == HomeSecurityEvent.StateIdle ? 0.0f : 1.0f;

                            return true;
                        }
                        // 0x06 Access Control
                        else if (type == NotificationType.AccessControl) // 0x06)
                        {
                            var mevent = (AccessControlEvent)notificationReport.mevent;
                            eventType = mevent switch
                            {
                                AccessControlEvent.WindowDoorIsOpen => Enums.EventKey.STATE_CLOSED, // 0x16
                                AccessControlEvent.WindowDoorIsClosed => Enums.EventKey.STATE_CLOSED, // 0x17
                                _ => Enums.EventKey.UNKNOWN,
                            };

                            floatVal = mevent == AccessControlEvent.WindowDoorIsClosed ? 1.0f : 0.0f;
                            return true;
                        }
                        else
                        {
                            goto default;
                            //eventType = Enums.EventKey.UNKNOWN;
                            //floatVal = -1;
                            //return false;
                        }
                    }
                case COMMAND_CLASS_SENSOR_BINARY_V2.SENSOR_BINARY_REPORT binaryReport:
                    {
                        eventType = Enums.EventKey.BINARY;
                        floatVal = binaryReport.sensorValue == 255 ? 1.0f : 0.0f;

                        return true;
                    }
                case COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION wakeUpNotification:
                    {
                        eventType = Enums.EventKey.WAKEUP;
                        floatVal = 1.0f;
                        return true;
                    }
                case COMMAND_CLASS_BATTERY.BATTERY_REPORT batteryReport:
                    {
                        eventType = Enums.EventKey.BATTERY;
                        floatVal = batteryReport.batteryLevel;
                        return true;
                    }
                case COMMAND_CLASS_BASIC_V2.BASIC_REPORT basicReport:
                    {
                        eventType = Enums.EventKey.STATE_ON;
                        floatVal = basicReport.currentValue == 255 ? 1.0f : 0.0f;
                        return true;
                    }
                case COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT binaryReport:
                    {
                        eventType = Enums.EventKey.STATE_ON;
                        floatVal = binaryReport.currentValue == 255 ? 1.0f : 0.0f;
                        return true;
                    }
                case COMMAND_CLASS_BASIC_V2.BASIC_SET basicSet:
                    {
                        eventType = Enums.EventKey.BASIC;
                        floatVal = basicSet.value == 255 ? 1.0f : 0.0f;
                        return true;
                    }
                case COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_REPORT report:
                    {
                        floatVal = -1;
                        eventType = Enums.EventKey.UNKNOWN;
                        const byte Heating = 1;
                        const byte Celcius = 0; //Fahrenheit = 1;
                        if (report.properties1.setpointType == Heating && report.properties2.scale == Celcius)
                        {
                            eventType = Enums.EventKey.THERMOSTAT_SETPOINT;
                            var precision = report.properties2.precision;
                            //value is MSB First (Big Endian), 1, 2 or 4 bytes
                            floatVal = Tools.GetInt32(report.value.ToArray());
                            //The setpoint temperature in Celcius
                            floatVal /= MathF.Pow(10f, precision);
                            return true;
                        }
                        return false;
                    }
                case COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT report:
                    {
                        //0: Idle, 1: Heating. Changes depending on the temperature
                        floatVal = report.properties1.operatingState;
                        eventType = Enums.EventKey.THERMOSTAT_OPERATING_STATE;
                        return true;
                    }
                case COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_REPORT report:
                    {
                        //0: OFF, 1: HEAT. Normally 1
                        floatVal = report.properties1.mode;
                        eventType = Enums.EventKey.THERMOSTAT_MODE;
                        return true;
                    }
                case COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP multiChannelReport:
                    {
                        eventType = Enums.EventKey.UNKNOWN;
                        if (multiChannelReport.commandClass == COMMAND_CLASS_BASIC_V2.ID
                            && multiChannelReport.command == COMMAND_CLASS_BASIC_V2.BASIC_REPORT.ID)
                        {
                            if (multiChannelReport.properties2.destinationEndPoint == 1)
                            {
                                eventType = Enums.EventKey.CHANNEL_1_STATE;
                            }
                            else if (multiChannelReport.properties2.destinationEndPoint == 2)
                            {
                                eventType = Enums.EventKey.CHANNEL_2_STATE;
                            }
                        }
                        if (eventType == Enums.EventKey.UNKNOWN)
                        {
                            floatVal = -1;
                            return false;
                        }
                        else
                        {
                            floatVal = multiChannelReport.parameter[0] == 255 ? 1.0f : 0.0f;
                            return true;
                        }
                    }
                case COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_NOTIFICATION notification:
                    {
                        floatVal = notification.sceneNumber;
                        eventType = Enums.EventKey.SCENE;
                        return true;
                    }
                default:
                    floatVal = -1;
                    eventType = Enums.EventKey.UNKNOWN;
                    return false;
            }
        }

        //public static bool GetKeyValue(this COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT notificationReport, out Enums.EventKey eventType, out float floatVal)
        //{
        //    var type = notificationReport.notificationType;
        //    var mevent = notificationReport.mevent;
        //    if(type == 0x07)
        //    {
        //        eventType = mevent switch
        //        {
        //            0x00 => Enums.EventKey.IDLE,
        //            0x03 => Enums.EventKey.TAMPER,
        //            0x04 => Enums.EventKey.TAMPER,
        //            0x09 => Enums.EventKey.TAMPER,
        //            0x07 => Enums.EventKey.MOTION,
        //            0x08 => Enums.EventKey.MOTION,
        //            0x0a => Enums.EventKey.IMPACT,
        //            _ => Enums.EventKey.UNKNOWN,
        //        };
        //        floatVal = 1;
        //        return true;
        //    } else
        //    {
        //        eventType = Enums.EventKey.UNKNOWN;
        //        floatVal = -1;
        //        return false;
        //    }

        //}
    }
}