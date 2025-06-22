namespace EspDataServer.Models;

public class SensorData
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public float Temperature { get; set; }

    public float Humidity { get; set; }
}