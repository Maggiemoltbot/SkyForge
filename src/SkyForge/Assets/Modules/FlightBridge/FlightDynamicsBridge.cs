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
    
    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int fdmPacketsSent = 0;
    [SerializeField] private int pwmPacketsReceived = 0;
    
    // UDP clients
    private UdpClient fdmSender;
    private UdpClient pwmReceiver;
    
    // Data handling
    private IPEndPoint pwmEndPoint;
    private ConcurrentQueue<PWMPacket> pwmPacketQueue = new ConcurrentQueue<PWMPacket>();
    private DateTime lastPWMReceivedTime = DateTime.MinValue;
    
    // Timing
    private const float CONNECTION_TIMEOUT = 2.0f; // seconds
    
    void OnEnable()
    {
        try
        {
            Debug.Log("Initializing Flight Dynamics Bridge...");
            
            // Initialize FDM sender
            fdmSender = new UdpClient();
            fdmSender.Connect(config.bfSITLIPAddress, config.fdmSendPort);
            Debug.Log($"FDM sender connected to {config.bfSITLIPAddress}:{config.fdmSendPort}");
            
            // Initialize PWM receiver
            pwmReceiver = new UdpClient(config.pwmReceivePort);
            pwmEndPoint = new IPEndPoint(IPAddress.Any, config.pwmReceivePort);
            
            // Start async receive operation
            pwmReceiver.BeginReceive(OnPWMDataReceived, null);
            Debug.Log($"PWM receiver listening on port {config.pwmReceivePort}");
            
            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing Flight Dynamics Bridge: {e.Message}");
            isConnected = false;
        }
    }
    
    void OnDisable()
    {
        Debug.Log("Shutting down Flight Dynamics Bridge...");
        
        // Close UDP clients
        if (fdmSender != null)
        {
            fdmSender.Close();
            fdmSender = null;
        }
        
        if (pwmReceiver != null)
        {
            pwmReceiver.Close();
            pwmReceiver = null;
        }
        
        isConnected = false;
        fdmPacketsSent = 0;
        pwmPacketsReceived = 0;
    }
    
    void FixedUpdate()
    {
        // Check connection status
        CheckConnectionTimeout();
        
        // Send FDM packet at fixed interval
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
        if (!isConnected || droneController == null || fdmSender == null)
            return;
            
        try
        {
            FDMPacket packet = new FDMPacket
            {
                timestamp = Time.time,
                
                // Convert angular velocity to NED
                imuAngularVelX = droneController.angularVelocity.x,
                imuAngularVelY = droneController.angularVelocity.y,
                imuAngularVelZ = droneController.angularVelocity.z,
                
                // Convert linear acceleration to NED
                imuLinearAccX = droneController.linearAcceleration.x,
                imuLinearAccY = droneController.linearAcceleration.y,
                imuLinearAccZ = droneController.linearAcceleration.z,
                
                // Convert quaternion to NED
                quatW = droneController.attitude.w,
                quatX = droneController.attitude.x,
                quatY = droneController.attitude.y,
                quatZ = droneController.attitude.z,
                
                // Convert velocity to NED
                velX = droneController.velocity.x,
                velY = droneController.velocity.y,
                velZ = droneController.velocity.z,
                
                // Convert position to NED
                posX = droneController.position.x,
                posY = droneController.position.y,
                posZ = droneController.position.z,
                
                pressure = droneController.pressure
            };
            
            // Serialize packet to byte array
            byte[] data = StructToBytes(packet);
            
            // Send packet
            fdmSender.Send(data, data.Length);
            fdmPacketsSent++;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending FDM packet: {e.Message}");
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
    /// </summary>
    private void ProcessReceivedPWMPackets()
    {
        while (pwmPacketQueue.TryDequeue(out PWMPacket packet))
        {
            if (droneController != null)
            {
                // Apply motor controls to drone controller
                droneController.motorPWM[0] = packet.motor1;
                droneController.motorPWM[1] = packet.motor2;
                droneController.motorPWM[2] = packet.motor3;
                droneController.motorPWM[3] = packet.motor4;
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
}