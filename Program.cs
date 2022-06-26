Console.WriteLine("Vibration monitoring starting up!");

var deviceClient = await CreateDeviceClient();
var handler = new VibrationMonitoringHandler(deviceClient);

await handler.Initialize();
await handler.Run();

async Task<DeviceClient> CreateDeviceClient()
{
    var connectionString = await File.ReadAllTextAsync(".key");
    var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
    await deviceClient.OpenAsync();

    return deviceClient;
}
