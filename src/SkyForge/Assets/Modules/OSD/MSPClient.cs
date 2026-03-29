using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class MSPClient : MonoBehaviour
{
    [Header("Connection")]
    public string host = "127.0.0.1";
    public int port = 5761;
    public float updateInterval = 0.1f; // 100ms

    [Header("Debug")]
    public bool enableDebug = true;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private bool isConnected = false;
    private float nextUpdateTime = 0f;

    // MSP Command IDs
    private const byte MSP_STATUS = 101;
    private const byte MSP_ANALOG = 110;
    private const byte MSP_ALTITUDE = 109;
    private const byte MSP_RC = 105;

    void Start()
    {
        Connect();
    }

    void Update()
    {
        if (!isConnected && Time.time > nextUpdateTime)
        {
            Connect();
            nextUpdateTime = Time.time + 1f; // Retry every second
        }

        if (isConnected && Time.time >= nextUpdateTime)
        {
            RequestData();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private async void Connect()
    {
        if (isConnected) return;

        try
        {
            tcpClient?.Close();
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port);
            stream = tcpClient.GetStream();
            isConnected = true;
            Debug.Log("MSPClient: Connected to Betaflight SITL");
            nextUpdateTime = Time.time + updateInterval;
        }
        catch (Exception e)
        {
            if (enableDebug)
                Debug.LogWarning($"MSPClient: Connection failed: {e.Message}");
            isConnected = false;
        }
    }

    private async void RequestData()
    {
        if (!isConnected) return;

        try
        {
            await SendRequest(MSP_STATUS);
            await Task.Delay(5);
            await SendRequest(MSP_ANALOG);
            await Task.Delay(5);
            await SendRequest(MSP_ALTITUDE);
            await Task.Delay(5);
            await SendRequest(MSP_RC);
        }
        catch (Exception e)
        {
            Debug.LogError($"MSPClient: Request failed: {e.Message}");
            isConnected = false;
        }
    }

    private async Task SendRequest(byte command)
    {
        byte[] packet = BuildRequestPacket(command);
        await stream.WriteAsync(packet, 0, packet.Length);
        await stream.FlushAsync();
        
        // Read response
        await ReadResponse();
    }

    private byte[] BuildRequestPacket(byte command)
    {
        byte[] packet = new byte[6];
        packet[0] = 36; // $
        packet[1] = 77; // M
        packet[2] = 60; // <
        packet[3] = 0;  // Size
        packet[4] = command;
        packet[5] = CalculateChecksum(packet, 3, 5);
        return packet;
    }

    private byte CalculateChecksum(byte[] data, int start, int end)
    {
        byte checksum = 0;
        for (int i = start; i <= end; i++)
        {
            checksum ^= data[i];
        }
        return checksum;
    }

    private async Task ReadResponse()
    {
        try
        {
            // Read header: $M>
            byte[] header = new byte[3];
            int bytesRead = 0;
            while (bytesRead < 3)
            {
                int read = await stream.ReadAsync(header, bytesRead, 3 - bytesRead);
                if (read == 0) break;
                bytesRead += read;
            }

            if (header[0] != 36 || header[1] != 77 || header[2] != 62) // $M>
            {
                Debug.LogWarning("Invalid MSP response header");
                return;
            }

            // Read size and command
            byte[] sizeCmd = new byte[2];
            await stream.ReadAsync(sizeCmd, 0, 2);
            byte size = sizeCmd[0];
            byte command = sizeCmd[1];

            // Read payload
            byte[] payload = new byte[size];
            await stream.ReadAsync(payload, 0, size);

            // Read checksum
            byte[] checksum = new byte[1];
            await stream.ReadAsync(checksum, 0, 1);

            // Validate checksum
            byte calculatedChecksum = CalculateChecksum(payload, 0, size - 1);
            calculatedChecksum ^= command;
            calculatedChecksum ^= size;
            
            if (calculatedChecksum != checksum[0])
            {
                Debug.LogWarning("MSP checksum mismatch");
                return;
            }

            // Parse data
            ParseResponse(command, payload);
        }
        catch (Exception e)
        {
            Debug.LogError($"MSPClient: Read response failed: {e.Message}");
            isConnected = false;
        }
    }

    private void ParseResponse(byte command, byte[] payload)
    {
        switch (command)
        {
            case MSP_STATUS:
                ParseStatus(payload);
                break;
            case MSP_ANALOG:
                ParseAnalog(payload);
                break;
            case MSP_ALTITUDE:
                ParseAltitude(payload);
                break;
            case MSP_RC:
                ParseRC(payload);
                break;
        }
    }

    private void ParseStatus(byte[] payload)
    {
        if (payload.Length < 12) return;
        
        // Skip cyccnt and i2c errors (4 + 2 bytes)
        // Flight mode flags (4 bytes) - we'll use later
        // Arm status is in the 11th byte (index 10)
        byte armState = payload[10];
        bool isArmed = (armState & 0x01) == 1;
        
        // Notify OSDData
        if (OSDData.Instance != null)
        {
            OSDData.Instance.SetArmedStatus(isArmed);
        }
    }

    private void ParseAnalog(byte[] payload)
    {
        if (payload.Length < 5) return;
        
        float voltage = payload[0] / 10.0f;
        int mah = BitConverter.ToUInt16(payload, 3);
        
        // Notify OSDData
        if (OSDData.Instance != null)
        {
            OSDData.Instance.SetBatteryData(voltage, 0, mah);
        }
    }

    private void ParseAltitude(byte[] payload)
    {
        if (payload.Length < 7) return;
        
        int altitudeCm = BitConverter.ToInt32(payload, 0);
        float altitude = altitudeCm / 100.0f; // cm to meters
        
        // variometer in cm/s
        int variometer = BitConverter.ToInt16(payload, 6);
        float verticalSpeed = variometer / 100.0f; // cm/s to m/s
        
        // Notify OSDData
        if (OSDData.Instance != null)
        {
            OSDData.Instance.SetAltitudeData(altitude, verticalSpeed);
        }
    }

    private void ParseRC(byte[] payload)
    {
        if (payload.Length < 8) return;
        
        // Channel 4 is usually flight mode
        ushort modeChannel = BitConverter.ToUInt16(payload, 6); // Channel 4
        
        string flightMode = GetFlightMode(modeChannel);
        
        // Notify OSDData
        if (OSDData.Instance != null)
        {
            OSDData.Instance.SetFlightMode(flightMode);
        }
    }

    private string GetFlightMode(ushort channelValue)
    {
        // Map channel values to Betaflight flight modes
        if (channelValue < 1100) return "FAILSAFE";
        else if (channelValue < 1300) return "MANUAL";
        else if (channelValue < 1500) return "RATTITUDE";
        else if (channelValue < 1700) return "ANGLE";
        else return "ACRO";
    }

    void OnDisable()
    {
        Disconnect();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    private void Disconnect()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }
        isConnected = false;
    }
}