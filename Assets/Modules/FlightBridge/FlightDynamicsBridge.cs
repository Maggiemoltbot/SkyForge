using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Main bridge component for communication between Unity and Betaflight SITL
/// Handles sending FDM packets and receiving PWM packets via UDP
/// </summary>
public class FlightDynamicsBridge : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BridgeConfig config;
    [SerializeField] private DroneController droneController;

    // Cache für RC-Send-Endpunkt
    private IPEndPoint rcEndPoint;
    
    [Header("IMU Simulation")]
    [SerializeField] private IMUSimulator imuSimulator;
    
    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int fdmPacketsSent = 0;
    private float lastErrorLogTime = -999f;
    [SerializeField] private int pwmPacketsReceived = 0;
    
    // UDP clients
    private UdpClient fdmSender;
    private UdpClient pwmReceiver;
    private UdpClient rcSender; // Für RC-Kanäle an BF SITL
    
    // Data handling
    private IPEndPoint pwmEndPoint;
    private readonly ConcurrentQueue<PWMPacket> pwmPacketQueue = new ConcurrentQueue<PWMPacket>();
    private DateTime lastPWMReceivedTime = DateTime.MinValue;
    
    // Timing
    private const float CONNECTION_TIMEOUT = 2.0f; // seconds
    
    void OnEnable()
    {
        InitializeBridge();
    }
    
    private void InitializeBridge()
    {
        CleanupSockets();
        isConnected = false;
        fdmPacketsSent = 0;
        pwmPacketsReceived = 0;
        lastPWMReceivedTime = DateTime.MinValue;

        if (config == null)
        {
            Debug.LogError("FlightDynamicsBridge is missing BridgeConfig reference.");
            return;
        }

        if (!IPAddress.TryParse(config.bfSITLIPAddress, out var sitlAddress))
        {
            Debug.LogError($"FlightDynamicsBridge received invalid SITL IP address: {config.bfSITLIPAddress}");
            return;
        }

        try
        {
            Debug.Log("Initializing Flight Dynamics Bridge...");

            // Initialize FDM sender
            fdmSender = new UdpClient(AddressFamily.InterNetwork);
            fdmSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            fdmSender.Connect(sitlAddress, config.fdmSendPort);
            Debug.Log($"FDM sender connected to {sitlAddress}:{config.fdmSendPort}");

            // Initialize PWM receiver
            pwmReceiver = new UdpClient(AddressFamily.InterNetwork);
            pwmReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            pwmReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, config.pwmReceivePort));
            pwmEndPoint = new IPEndPoint(IPAddress.Any, config.pwmReceivePort);

            // Start async receive operation
            pwmReceiver.BeginReceive(OnPWMDataReceived, null);
            Debug.Log($"PWM receiver listening on port {config.pwmReceivePort}");

            // Initialize RC sender
            rcSender = new UdpClient(AddressFamily.InterNetwork);
            rcSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            rcEndPoint = new IPEndPoint(sitlAddress, config.rcSendPort);
            rcSender.Connect(rcEndPoint);
            Debug.Log($"RC sender initialized for {sitlAddress}:{config.rcSendPort}");

            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing Flight Dynamics Bridge: {e.Message}");
            CleanupSockets();
            isConnected = false;
        }
    }
    
    void OnDisable()
    {
        Debug.Log("Shutting down Flight Dynamics Bridge...");
        CleanupSockets();
        rcEndPoint = null;
        isConnected = false;
        fdmPacketsSent = 0;
        pwmPacketsReceived = 0;
        lastPWMReceivedTime = DateTime.MinValue;
    }

    private void CleanupSockets()
    {
        CloseClient(ref fdmSender);
        CloseClient(ref pwmReceiver);
        CloseClient(ref rcSender);
    }

    private void CloseClient(ref UdpClient client)
    {
        if (client != null)
        {
            try
            {
                client.Close();
            }
            catch (Exception)
            {
                // ignore during shutdown
            }
            finally
            {
                client = null;
            }
        }
    }
    
    void FixedUpdate()
    {
        // Check connection status
        CheckConnectionTimeout();
        
        // Always send FDM — even when SITL hasn't responded yet.
        // This bootstraps the loop: FDM → SITL → PWM → Unity physics → FDM
        SendFDMPacket();
    }
    
    void Update()
    {
        // Process received PWM packets
        ProcessReceivedPWMPackets();
    }
    
    /// <summary>
    /// Sends FDM packet with current drone state to Betaflight SITL
    /// </summary>
    private void SendFDMPacket()
    {
        // NOTE: Do NOT gate on isConnected — we must keep sending FDM
        // so SITL can (re)start the PWM loop even after a timeout.
        if (droneController == null || fdmSender == null)
            return;
            
        try
        {
            // Get IMU data from simulator if available, otherwise fallback to drone controller
            Vector3 angularVelocity = Vector3.zero;
            Vector3 linearAcceleration = Vector3.zero;
            
            if (imuSimulator != null)
            {
                angularVelocity = imuSimulator.GetGyroData();
                linearAcceleration = imuSimulator.GetAccelerometerData();
            }
            else
            {
                // Fallback to drone controller data
                angularVelocity = droneController.CurrentAngularVelocity;
                linearAcceleration = droneController.CurrentLinearAcceleration;
            }
            
            Vector3 nedAngularVelocity = CoordinateConverter.AngularVelocityUnityToNED(angularVelocity);
            Vector3 nedLinearAcceleration = CoordinateConverter.LinearAccelerationUnityToNED(linearAcceleration);
            Vector3 nedVelocity = CoordinateConverter.UnityToNED(droneController.CurrentVelocity);
            Vector3 nedPosition = CoordinateConverter.UnityToNED(droneController.CurrentPosition);
            Quaternion nedAttitude = CoordinateConverter.QuaternionUnityToNED(droneController.CurrentAttitude);

            FDMPacket packet = new FDMPacket
            {
                timestamp = Time.time,
                
                // Angular velocity in NED
                imuAngularVelX = nedAngularVelocity.x,
                imuAngularVelY = nedAngularVelocity.y,
                imuAngularVelZ = nedAngularVelocity.z,
                
                // Linear acceleration in NED
                imuLinearAccX = nedLinearAcceleration.x,
                imuLinearAccY = nedLinearAcceleration.y,
                imuLinearAccZ = nedLinearAcceleration.z,
                
                // Attitude quaternion in NED
                quatW = nedAttitude.w,
                quatX = nedAttitude.x,
                quatY = nedAttitude.y,
                quatZ = nedAttitude.z,
                
                // Velocity in NED
                velX = nedVelocity.x,
                velY = nedVelocity.y,
                velZ = nedVelocity.z,
                
                // Position in NED
                posX = nedPosition.x,
                posY = nedPosition.y,
                posZ = nedPosition.z,
                
                pressure = droneController.CurrentPressure
            };
            
            // Serialize packet to byte array
            byte[] data = StructToBytes(packet);
            
            // Send packet
            fdmSender.Send(data, data.Length);
            fdmPacketsSent++;

            if (fdmPacketsSent % 400 == 0)
            {
                Debug.Log($"[FDM NED] pos=({nedPosition.x:F2},{nedPosition.y:F2},{nedPosition.z:F2}) " +
                          $"vel=({nedVelocity.x:F2},{nedVelocity.y:F2},{nedVelocity.z:F2}) " +
                          $"gyro=({nedAngularVelocity.x:F2},{nedAngularVelocity.y:F2},{nedAngularVelocity.z:F2})");
            }
        }
        catch (Exception e)
        {
            // Rate-limit error logging to once every 5 seconds
            if (Time.time - lastErrorLogTime > 5f)
            {
                Debug.LogWarning($"[SkyForge] FDM send failed (SITL running?): {e.Message}");
                lastErrorLogTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Callback for receiving PWM data from Betaflight SITL
    /// </summary>
    private void OnPWMDataReceived(IAsyncResult result)
    {
        try
        {
            if (pwmReceiver == null) return;
            
            // Get received data
            byte[] data = pwmReceiver.EndReceive(result, ref pwmEndPoint);
            
            // Parse PWM packet
            PWMPacket packet = BytesToStruct<PWMPacket>(data);
            
            // Add to queue for main thread processing
            pwmPacketQueue.Enqueue(packet);
            pwmPacketsReceived++;
            lastPWMReceivedTime = DateTime.Now;
            
            // Begin receiving next packet
            if (pwmReceiver != null)
                pwmReceiver.BeginReceive(OnPWMDataReceived, null);
        }
        catch (ObjectDisposedException)
        {
            // This happens when the UDP client is closed during shutdown
            // Normal behavior, don't log as error
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving PWM packet: {e.Message}");
        }
    }
    
    /// <summary>
    /// Processes received PWM packets on the main thread
    /// Applies SITL→Unity motor remapping (BF motor order differs from Unity FL/FR/BL/BR)
    /// </summary>
    private void ProcessReceivedPWMPackets()
    {
        while (pwmPacketQueue.TryDequeue(out PWMPacket packet))
        {
            if (droneController != null)
            {
                // SITL sends motor_speed[0]=BF1, [1]=BF2, [2]=BF3, [3]=BF0
                // Unity expects FL=0, FR=1, BL=2, BR=3
                // Use MotorModel.RemapSitlMotorOrder for correct mapping
                MotorModel.RemapSitlMotorOrder(
                    droneController.motorPWM,
                    packet.motor1,  // SITL slot 0 = BF1
                    packet.motor2,  // SITL slot 1 = BF2
                    packet.motor3,  // SITL slot 2 = BF3
                    packet.motor4   // SITL slot 3 = BF0
                );

                if (pwmPacketsReceived % 400 == 0)
                {
                    Debug.Log($"[PWM] motors: FL={droneController.motorPWM[0]:F3} FR={droneController.motorPWM[1]:F3} BL={droneController.motorPWM[2]:F3} BR={droneController.motorPWM[3]:F3}");
                }
            }
        }
    }
    
    /// <summary>
    /// Checks for connection timeout based on last PWM packet received
    /// </summary>
    private void CheckConnectionTimeout()
    {
        if (lastPWMReceivedTime != DateTime.MinValue)
        {
            float timeSinceLastPacket = (float)(DateTime.Now - lastPWMReceivedTime).TotalSeconds;
            isConnected = timeSinceLastPacket < CONNECTION_TIMEOUT;
        }
    }
    
    /// <summary>
    /// Converts a struct to byte array using Marshal
    /// </summary>
    private byte[] StructToBytes<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] arr = new byte[size];
        
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        
        return arr;
    }
    
    /// <summary>
    /// Converts byte array to struct using Marshal
    /// </summary>
    private T BytesToStruct<T>(byte[] data) where T : struct
    {
        T structure = new T();
        int size = Marshal.SizeOf(structure);
        
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(data, 0, ptr, size);
        structure = (T)Marshal.PtrToStructure(ptr, structure.GetType());
        Marshal.FreeHGlobal(ptr);
        
        return structure;
    }
    
    /// <summary>
    /// Gets the current connection status
    /// </summary>
    public bool IsConnected => isConnected;
    
    /// <summary>
    /// Gets the number of FDM packets sent
    /// </summary>
    public int FDMPacketsSent => fdmPacketsSent;
    
    /// <summary>
    /// Gets the number of PWM packets received
    /// </summary>
    public int PWMPacketsReceived => pwmPacketsReceived;

    /// <summary>
    /// Sends RC channel values (array of 16 ints) to Betaflight SITL
    /// </summary>
    /// <param name="channels">Array of 16 channel values in microseconds (typically 1000-2000)</param>
    public void SendRCChannels(int[] channels)
    {
        if (!isConnected || channels == null || channels.Length < 16 || rcSender == null || rcEndPoint == null)
            return;
        
        try
        {
            // Create RCPacket
            RCPacket packet = new RCPacket
            {
                timestamp = Time.time
            };
            
            // Copy channel values (converting int to ushort)
            for (int i = 0; i < 16; i++)
            {
                packet.SetChannel(i, (ushort)channels[i]);
            }
            
            // Serialize and send
            byte[] data = StructToBytes(packet);
            rcSender.Send(data, data.Length);
        }
        catch (Exception e)
        {
            if (Time.time - lastErrorLogTime > 5f)
            {
                Debug.LogWarning($"[SkyForge] RC send failed: {e.Message}");
                lastErrorLogTime = Time.time;
            }
        }
    }
}
