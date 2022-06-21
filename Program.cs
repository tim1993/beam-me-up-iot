using System.Device.Spi;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Iot.Device.Adxl345;

Console.WriteLine("Vibration monitoring starting up!");

var deviceClient = await CreateDeviceClient();
var sensor = InitializeSensor();

Console.WriteLine("Starting telemetry transmission.");
while (true)
{
    await ReadSensorAndSendTelemetry(deviceClient, sensor);
    Console.WriteLine("Telemetry sent. Delaying...");

    await Task.Delay(TimeSpan.FromSeconds(10));
}

async Task<DeviceClient> CreateDeviceClient()
{
    var connectionString = await File.ReadAllTextAsync(".key");
    var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
    await deviceClient.OpenAsync();

    return deviceClient;
}

Adxl345 InitializeSensor()
{
    var spiBinding = SpiDevice.Create(new SpiConnectionSettings(0, 0)
    {
        ClockFrequency = Adxl345.SpiClockFrequency,
        Mode = Adxl345.SpiMode
    });
    var sensor = new Adxl345(spiBinding, GravityRange.Range08);

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