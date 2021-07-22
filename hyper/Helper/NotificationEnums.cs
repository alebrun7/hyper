namespace hyper.Helper
{

    // SDS13713-Notification-Command-Class.xlsx
    // https://www.silabs.com/documents/login/miscellaneous/SDS13713-Notification-Command-Class.xlsx
    public enum NotificationType : byte
    {
        SmokeAlarm = 0x01,
        COAlarm = 0x02,
        CO2Alarm = 0x03,
        HeatAlarm = 0x04,
        WaterAlarm = 0x05,
        AccessControl = 0x06,
        HomeSecurity = 0x07,
        PowerManagement = 0x08,
        System = 0x09,
        EmergencyAlarm = 0x0A,
        Clock = 0x0B,
        Appliance = 0x0C,
        HomeHealth = 0x0D,
        Siren = 0x0E,
        WaterValve = 0x0F,
        WeatherAlarm = 0x10,
        Irrigation = 0x11,
        GasAlarm = 0x12,
        PestControl = 0x13,
        LightSensor = 0x14,
        WaterQualityMonitoring = 0x15,
        HomeMonitoring = 0x16,
        RequestPendingNotification = 0xFF,
    }

    //note: in the description they are event or state
    //for example StatIdle, WindowDoorIsOpen are states in the list
    //but I find it simplier and more readable to call them all events here
    public enum HomeSecurityEvent : byte
    {
        StateIdle = 0x00,
        IntrusionStateDetected = 0x01,
        Intrusion = 0x02,
        TamperingProductCoverRemoved = 0x03,
        TamperingInvalidCode = 0x04,
        GlassBreakageLocationProvided = 0x05,
        GlassBreakage = 0x06,
        MotionDetectionLocationProvided = 0x07,
        MotionDetection = 0x08,
        TamperingProductMoved = 0x09,
        ImpactDetected = 0x0A,
        MagneticFieldInterferenceDetected = 0x0B,
        RFJammingDetected = 0x0C,
        UnknownEvent = 0xFE,
    }

    public enum AccessControlEvent : byte
    {
        StateIdle = 0x00,
        ManualLockOperation = 0x01,
        ManualUnlockOperation = 0x02,
        RFLockOperation = 0x03,
        RFUnlockOperation = 0x04,
        KeypadLockOperation = 0x05,
        KeypadUnlockOperation = 0x06,
        ManualNotFullyLockedOperation = 0x07,
        RFNotFullyLockedOperation = 0x08,
        AutoLockLockedOperation = 0x09,
        AutoLockNotFullyLockedOperation = 0x0A,
        LockJammed = 0x0B,
        AllUserCodesDeleted = 0x0C,
        SingleUserCodeDeleted = 0x0D,
        NewUserCodeAdded = 0x0E,
        NewUserCodeNotAddedDueToDuplicateCode = 0x0F,
        KeypadTemporaryDisabled = 0x10,
        KeypadBusy = 0x11,
        NewProgramCodeEntered = 0x12,
        ManuallyEnterUserAccessCodeExceedsCodeLimit = 0x13,
        UnlockByRFWithInvalidUserCode = 0x14,
        LockedByRFWithInvalidUserCode = 0x15,
        WindowDoorIsOpen = 0x16,
        WindowDoorIsClosed = 0x17,
        WindowDoorHandleIsOpen = 0x18,
        WindowDoorHandleIsClosed = 0x19,
        MessagingUserCodeEnteredViaKeypad = 0x20,
        BarrierPerformingInitializationProcess = 0x40,
        BarrierOperationForceHasBeenExceeded = 0x41,
        BarrierMotorHasExceededManufacturerSOperationalTimeLimit = 0x42,
        BarrierOperationHasExceededPhysicalMechanicalLimits = 0x43,
        BarrierUnableToPerformRequestedOperationDueToULRequirements = 0x44,
        BarrierUnattendedOperationHasBeenDisabledPerULRequirements = 0x45,
        BarrierFailedToPerformRequestedOperation, DeviceMalfunction = 0x46,
        BarrierVacationMode = 0x47,
        BarrierSafetyBeamObstacle = 0x48,
        BarrierSensorNotDetectedSupervisoryError = 0x49,
        BarrierSensorLowBatteryWarning = 0x4A,
        BarrierDetectedShortInWallStationWires = 0x4B,
        BarrierAssociatedWithNonZWaveRemoteControl = 0x4C,
        UnknownEventState = 0xFE,
    }

}