using System.Device.Spi;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Iot.Device.Adxl345;

Console.WriteLine("Vibration monitoring starting up!");

var deviceClient = await CreateDeviceClient();
var sensor = InitializeSensor();


while (true)
{
    var message = CreateTelemetryFromReading(sensor.Acceleration);
    await deviceClient.SendEventAsync(message);

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
    var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(acceleration));
    return new Message(payload);
}