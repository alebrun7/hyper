namespace hyper.Helper
{
    // Enums for Command Class Meter ( COMMAND_CLASS_METER_V5 )
    // source: Application Command Classes Specification at
    // https://z-wavealliance.org/z-wave-specifications/

    public enum MeterReporRateType : byte
    {
        Unspecified = 0x00, //unspecified
        Import = 0x01, //consumed
        Export = 0x02, //produced
        Reserved = 0x03,
    }

    public enum MeterReportMeterType : byte
    {
        ElectricMeter = 0x01,
        GasMeter = 0x02,
        WaterMeter = 0x03,
        HeatingMeter = 0x04,
        CoolingMeter = 0x05,
    }

    //only for meterType == ElectricMeter:
    public enum MeterReportScalebit2EM : byte
    {
        kWh = 0x00,
        kVAh = 0x01,
        W = 0x02,
        PulseCount = 0x03,
        V = 0x04,
        A = 0x05,
        PowerFactor = 0x06,
        MST = 0x07 //must read scalebit10
    }
}