using System.Device.Spi;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Iot.Device.Adxl345;
using Microsoft.Azure.Devices.Shared;

record VibrationMonitoringHandler(DeviceClient DeviceClient)
{
    private const string SamplingRateKey = "SamplingRate";
    private const string GravityRangeKey = "GravityRangeSetting";
    private CancellationTokenSource cts = new();
    private Adxl345? sensor;

    private TimeSpan delay = TimeSpan.FromSeconds(30);


    public async Task Initialize()
    {
        await DeviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleDesiredPropertiesUpdated, null);
        var twin = await DeviceClient.GetTwinAsync();
        await HandleDesiredPropertiesUpdated(twin.Properties.Desired, new object());
    }

    public async Task Run()
    {
        while (!cts.IsCancellationRequested
                && sensor is not null)
        {
            await ReadSensorAndSendTelemetry(DeviceClient, sensor);
            Console.WriteLine("Telemetry sent. Delaying...");
            await Task.Delay(delay);
        }
    }

    async Task HandleDesiredPropertiesUpdated(TwinCollection desiredProperties, object _)
    {
        Console.WriteLine("Received Desired Properties Update");

        await HandleGravityRangeFromDesiredProperties(desiredProperties);
        await HandleSamplingRateFromDesiredProperties(desiredProperties);
    }

    async Task HandleGravityRangeFromDesiredProperties(TwinCollection desiredProperties)
    {
        if (desiredProperties.Contains(GravityRangeKey))
        {
            var gravityRange = desiredProperties[GravityRangeKey]?.ToString();
            if (Enum.TryParse<GravityRange>(gravityRange, out GravityRange desiredRange))
            {
                Console.WriteLine($"Setting GravityRange to {desiredRange}");
                sensor = InitializeSensor(desiredRange);
                await ReportGravityRange(desiredRange);
            }
            else
            {
                Console.WriteLine($"Could not parse {gravityRange} to valid GravityRange");
            }
        }
    }

    async Task HandleSamplingRateFromDesiredProperties(TwinCollection desiredProperties)
    {
        if (desiredProperties.Contains(SamplingRateKey))
        {
            var samplingRate = desiredProperties[SamplingRateKey]?.ToString();
            if (int.TryParse(samplingRate, out int samplingRateValue))
            {
                Console.WriteLine($"Setting SamplingRate to {samplingRateValue}");
                delay = TimeSpan.FromSeconds(samplingRateValue);
                await ReportSamplingRate(samplingRateValue);
            }
            else
            {
                Console.WriteLine($"Could not parse {samplingRate} to valid SamplingRate");
            }
        }
    }


    Adxl345 InitializeSensor(GravityRange range)
    {
        var spiBinding = SpiDevice.Create(new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = Adxl345.SpiClockFrequency,
            Mode = Adxl345.SpiMode
        });
        var sensor = new Adxl345(spiBinding, range);

        return sensor;
    }

    Message CreateTelemetryFromReading(Vector3 acceleration)
    {
        var json = JsonSerializer.Serialize(new { x = acceleration.X, y = acceleration.Y, z = acceleration.Z });
        Console.WriteLine($"JSON-Payload: {json}");
        var payload = Encoding.UTF8.GetBytes(json);
        return new Message(payload);
    }

    async Task ReadSensorAndSendTelemetry(DeviceClient deviceClient, Adxl345 sensor)
    {
        var acceleration = sensor.Acceleration;
        Console.WriteLine($"Read sensor: {acceleration}");

        var message = CreateTelemetryFromReading(acceleration);
        await deviceClient.SendEventAsync(message);
    }

    async Task ReportGravityRange(GravityRange desiredRange) => await DeviceClient.UpdateReportedPropertiesAsync(new TwinCollection { ["GravityRangeSetting"] = desiredRange.ToString() });
    async Task ReportSamplingRate(int samplingRate) => await DeviceClient.UpdateReportedPropertiesAsync(new TwinCollection { ["SamplingRate"] = samplingRate });
}