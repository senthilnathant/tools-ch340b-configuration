// *********************************************************************************************************
//
//	   Project      : WCH CH340B Configuration Utility
//	   FileName     : CH340BDeviceAccess.cs
//	   Author       : SENTHILNATHAN THANGAVEL, INDEPENDENT DEVELOPER
//     Co-Author(s) : 
//	   Created      : ‎02 January, ‎2022
//
// *********************************************************************************************************
//
// Module Description
//
// This module has methods that will detect CH340B devices connected to PC USB port, read configuration data
// from the selected CH340B device, write configuration data to the selected CH340B device.
// *********************************************************************************************************
//
// History
//
// Date			        Version		Author		                Changes
//
// ‎02 January, ‎2022   	1.0.0		SENTHILNATHAN THANGAVEL		Initial version
//
// *********************************************************************************************************
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static CH340BConfigure.NativeAccess;

namespace CH340BConfigure
{
    public class CH340BDeviceAccess
    {
        // Device Info object array that has device's user friendly name, actual COM port name
        // This application supports multiple CH340B devices connected to the same PC's USB ports -
        // up to 10 number of devices
        internal DeviceInfo[] aoDevInfo = DeviceInfo.NewInitArray(10);

        // Allocate separate buffers for Read and Write functions
        private Byte[] abyReadData = new byte[1024];
        private Byte[] abyWriteData = new byte[1024];
        // Allocate a byte array buffer for using in the DeviceIoControl calls
        private Byte[] abyInputBuffer = null;
        private uint unBytesReturned = 0;
        // Device Handle
        private IntPtr pHandle = IntPtr.Zero;
        private IntPtr pspDeviceInterfaceData = IntPtr.Zero;
        private IntPtr pspDeviceInterfaceDetailData = IntPtr.Zero;       

        internal class DeviceInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szComportName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szComportNameForHandle;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDeviceName;

            public int nDeviceIndex;

            // Array of Device Info objects
            static internal DeviceInfo[] NewInitArray(ulong num)
            {
                DeviceInfo[] aoDeviceInfo = new DeviceInfo[num];
                for (ulong i = 0; i < num; i++)
                {
                    aoDeviceInfo[i] = new DeviceInfo();
                }
                return aoDeviceInfo;
            }
        }

        internal int FindDevices()
        {
            bool bResult = true;
            int nNumberOfDevices = 0;
            try
            {
                bResult = GetDevices(ref aoDevInfo, ref nNumberOfDevices);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            return nNumberOfDevices;
        }

        /* 
         * Function Name: GetDevices
         * Description: This function looks for the CH340B devices connected to the PC USB port and 
         * get the device friendly name, COM port name, which will be used in further device opening to perform Read / Write 
         * functions with the device.
         * Arguments: 
         * DeviceInfo[] - DeviceInfo object array - it will have the device info object for each device detected in the system
         * nTotalDevices - Integer value that represents the number of devices detected in the system
         * Return: boolean value - true if the function call is successsful, false if any error occurred
         * The code doesn't report error as of now. It checks for error using GetLastError, but not reporting to the caller.
         */
        private bool GetDevices(ref DeviceInfo[] aoDevInfo, ref int nTotalDevices)
        {
            bool bStatus = true;
            uint uintError = Constants.NO_ERROR;
            IntPtr hDevInfo = IntPtr.Zero;
            IntPtr hDevice = IntPtr.Zero;
            uint uintDeviceID = 0;
            uint uintDataT = 0;
            uint uintBufferSize = 0;
            // COM Port Interface Class GUID
            Guid sGuid = new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73");
            IntPtr psDeviceInfoData = IntPtr.Zero;
            NativeStructs.SP_DEVINFO_DATA sDeviceInfoData = new NativeStructs.SP_DEVINFO_DATA();
            IntPtr pPropertyBuffer = IntPtr.Zero;
            do
            {
                uintError = NativeAccess.Constants.NO_ERROR;
                nTotalDevices = 0;
                // Create a HDEVINFO with all the present devices of the COM Port Interface Class GUID Supplied.
                hDevInfo = NativeMethods.SetupDiGetClassDevs(ref sGuid, IntPtr.Zero, IntPtr.Zero,
                            Constants.DIGCF_DEVICEINTERFACE | Constants.DIGCF_PRESENT);
                // Validate the hDevInfo handle
                if (Constants.INVALID_HANDLE_VALUE == hDevInfo)
                {
                    uintError = Constants.ERROR_INVALID_HANDLE;	// (Long Pointer Value - 1)
                    bStatus = false;
                    break;
                }
                // Enumerate through all devices in the set
                sDeviceInfoData.cbSize = Marshal.SizeOf(sDeviceInfoData);
                // Initialize pointer for sDeviceInfoData
                psDeviceInfoData = Marshal.AllocHGlobal(Marshal.SizeOf(sDeviceInfoData));
                Marshal.StructureToPtr(sDeviceInfoData, psDeviceInfoData, true);
                for (uintDeviceID = 0; NativeMethods.SetupDiEnumDeviceInfo(hDevInfo, uintDeviceID, psDeviceInfoData);
                    uintDeviceID++)
                {
                    sDeviceInfoData = (NativeStructs.SP_DEVINFO_DATA)Marshal.PtrToStructure(psDeviceInfoData,
                        typeof(NativeStructs.SP_DEVINFO_DATA));
                    // Get the device friendly name from the registry
                    while (!NativeMethods.SetupDiGetDeviceRegistryProperty(hDevInfo, psDeviceInfoData,
                        (uint)Constants.SPDRP.SPDRP_FRIENDLYNAME, ref uintDataT, pPropertyBuffer, uintBufferSize, ref uintBufferSize))
                    {
                        // Check for error 
                        uintError = (uint)Marshal.GetLastWin32Error();
                        if (Constants.ERROR_INSUFFICIENT_BUFFER == uintError)
                        {
                            // If the error is insufficient buffer, then allocate the buffer for number of bytes specified by uintBufferSize
                            pPropertyBuffer = Marshal.AllocHGlobal((int)uintBufferSize);
                        }
                        else
                        {
                            // If the error is not ERROR_INSUFFICIENT_BUFFER, quit the loop
                            bStatus = false;
                            break;
                        }
                    }
                    IntPtr pDeviceName = new IntPtr(pPropertyBuffer.ToInt64());
                    String szDeviceName = Marshal.PtrToStringAuto(pDeviceName);
                    String szDevicePathName = System.String.Empty;
                    String szComportName = "";
                    try
                    {
                        szComportName = szDeviceName.Split(new char[] { '(', ')' })[1];
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                            MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    }
                    if (string.IsNullOrEmpty(szComportName) == false)
                    {
                        // Form the COM Port name like this "\\\\.\\COMn"
                        string szComportNameForHandle = "\\\\.\\" + szComportName;
                        // Check if the device detected is really CH340B or other Virtual / Real COM Port devices
                        bool bDeviceIsCh340B = CheckIfDeviceIsCh340B(szComportNameForHandle);
                        if (bDeviceIsCh340B)
                        {
                            // If this is really CH340B then, store its friendly name, COM Port name
                            // to the device info object
                            aoDevInfo[nTotalDevices].szComportName = szComportName;
                            aoDevInfo[nTotalDevices].szComportNameForHandle = szComportNameForHandle;
                            aoDevInfo[nTotalDevices].szDeviceName = szDeviceName;
                            aoDevInfo[nTotalDevices].nDeviceIndex = nTotalDevices;
                            nTotalDevices++;
                        }
                    }
                }

            } while (false);
            // Free the memory allocated from unmanaged memory used
            if (psDeviceInfoData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(psDeviceInfoData);
                psDeviceInfoData = IntPtr.Zero;
            }
            if (pPropertyBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pPropertyBuffer);
                pPropertyBuffer = IntPtr.Zero;
            }
            // Destroy the device information list
            if (hDevInfo != IntPtr.Zero)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(hDevInfo);
            }
            return bStatus;
        }

        /* 
         * Function Name: CheckIfDeviceIsCh340B
         * Description: This function checks whether the detected device is a CH340B or other COM port devices
         * Arguments: 
         * IntPtr - pHandle - Managed Pointer variable that represents the device handle     
         * Return: boolean value - true if the device detected is a CH340B or else false
         */
        private bool CheckIfDeviceIsCh340B(string szComportName)
        {
            bool bResult = false;
            bool bDeviceIsCh340B = false;
            try
            {
                // Open the device using the COM Port Name in the format "\\\\.\\COMn"
                pHandle = NativeMethods.CreateFile(szComportName, Constants.GENERIC_READ | Constants.GENERIC_WRITE,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, IntPtr.Zero);
                if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
                {
                    // Initialize the vendor interface
                    bResult = InitializeCh340BVendorInterface(pHandle);
                    if (bResult == true)
                    {
                        // Get Serial Properties
                        NativeStructs.SERIAL_COMMPROP sSerialCompProp = new NativeStructs.SERIAL_COMMPROP();
                        IntPtr psSerialCompProp = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialCompProp));
                        Marshal.StructureToPtr(sSerialCompProp, psSerialCompProp, false);
                        bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_PROPERTIES, IntPtr.Zero, 0, psSerialCompProp,
                            Marshal.SizeOf(sSerialCompProp), out unBytesReturned, IntPtr.Zero);
                        Marshal.PtrToStructure(psSerialCompProp, sSerialCompProp);
                        Marshal.FreeHGlobal(psSerialCompProp);

                        // The magic value 0x43485523 is returned in the unsigned integer ProvSpec2 of Serial Comm Prop structure by the CH340B driver
                        if (sSerialCompProp.ProvSpec2 == 0x43485523)
                        {
                            // Device is CH340B
                            bDeviceIsCh340B = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            // Close the device handle
            if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
            {
                NativeMethods.CloseHandle(pHandle);
            }
            return bDeviceIsCh340B;
        }

        /* 
         * Function Name: InitializeCh340BVendorInterface
         * Description: This function initializes the Vendor Interface of the CH340B driver
         * Arguments: 
         * IntPtr - pHandle - Manged Pointer variable that represents the device handle     
         * Return: boolean value - true if the device detected is a CH340B or else false
         */
        private bool InitializeCh340BVendorInterface(IntPtr pHandle)
        {
            bool bResult = false;

            try
            {
                // Validate the device hanlde
                if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
                {
                    // CH340B uses a vendor supplied driver on Microsoft Windows, that is its not fully complaint with 
                    // Microsoft's USB CDC driver, so the Microsoft USB CDC driver is not loaded and it requires
                    // the vendor - chip manufacturer (WCH) supplied driver.
                    // First the device must be informed to send the Vendor request command through Control Endpoint
                    // The following sequence of commands are issued to the CH340B's Kernel mode driver and make the CH340B driver
                    // to issue a vendor request through Control Endpoint to the CH340B.

                    // Get Serial Properties
                    NativeStructs.SERIAL_COMMPROP sSerialCompProp = new NativeStructs.SERIAL_COMMPROP();
                    IntPtr psSerialCompProp = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialCompProp));
                    Marshal.StructureToPtr(sSerialCompProp, psSerialCompProp, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_PROPERTIES, IntPtr.Zero, 0, psSerialCompProp,
                        Marshal.SizeOf(sSerialCompProp), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialCompProp, sSerialCompProp);
                    Marshal.FreeHGlobal(psSerialCompProp);

                    // Set Serial Queue size
                    NativeStructs.SERIAL_QUEUE_SIZE sSerialQueueSize = new NativeStructs.SERIAL_QUEUE_SIZE();
                    sSerialQueueSize.InSize = 0x00002000;
                    sSerialQueueSize.OutSize = 0x00002000;
                    IntPtr psSerialQueueSize = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialQueueSize));
                    Marshal.StructureToPtr(sSerialQueueSize, psSerialQueueSize, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_QUEUE_SIZE, psSerialQueueSize,
                        Marshal.SizeOf(sSerialQueueSize), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialQueueSize);

                    // Get Serial TimeOuts
                    NativeStructs.SERIAL_TIMEOUTS sSerialTimeOuts = new NativeStructs.SERIAL_TIMEOUTS();
                    IntPtr psSerialTimeOuts = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialTimeOuts));
                    Marshal.StructureToPtr(sSerialTimeOuts, psSerialTimeOuts, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_TIMEOUTS, IntPtr.Zero, 0, psSerialTimeOuts,
                        Marshal.SizeOf(sSerialTimeOuts), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialTimeOuts, sSerialTimeOuts);
                    Marshal.FreeHGlobal(psSerialTimeOuts);

                    // Set Serial Timeouts
                    sSerialTimeOuts = new NativeStructs.SERIAL_TIMEOUTS();
                    sSerialTimeOuts.ReadIntervalTimeout = 0x00000000;
                    sSerialTimeOuts.ReadTotalTimeoutMultiplier = 0x00000064;
                    sSerialTimeOuts.ReadTotalTimeoutConstant = 0x000003E8;
                    sSerialTimeOuts.WriteTotalTimeoutMultiplier = 0x00000032;
                    sSerialTimeOuts.WriteTotalTimeoutConstant = 0x000001F4;
                    psSerialTimeOuts = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialTimeOuts));
                    Marshal.StructureToPtr(sSerialTimeOuts, psSerialTimeOuts, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_TIMEOUTS, psSerialTimeOuts,
                        Marshal.SizeOf(sSerialTimeOuts), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialTimeOuts);

                    // Issue Serial Purge
                    abyInputBuffer = new byte[4];
                    abyInputBuffer[0] = 0x0F;
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_PURGE, abyInputBuffer, abyInputBuffer.Length,
                        IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);

                    // Get Serial Baudrate
                    NativeStructs.SERIAL_BAUD_RATE sSerialBaudRate = new NativeStructs.SERIAL_BAUD_RATE();
                    IntPtr psSerialBaudRate = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialBaudRate));
                    Marshal.StructureToPtr(sSerialBaudRate, psSerialBaudRate, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_BAUD_RATE, IntPtr.Zero, 0, psSerialBaudRate,
                        Marshal.SizeOf(sSerialBaudRate), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialBaudRate, sSerialBaudRate);
                    Marshal.FreeHGlobal(psSerialBaudRate);

                    // Get Serial Line control
                    NativeStructs.SERIAL_LINE_CONTROL sSerialLineControl = new NativeStructs.SERIAL_LINE_CONTROL();
                    IntPtr psSerialLineControl = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialLineControl));
                    Marshal.StructureToPtr(sSerialLineControl, psSerialLineControl, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_LINE_CONTROL, IntPtr.Zero, 0, psSerialLineControl,
                        Marshal.SizeOf(sSerialLineControl), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialLineControl, sSerialLineControl);
                    Marshal.FreeHGlobal(psSerialLineControl);

                    // Get Serial CHARS
                    NativeStructs.SERIAL_CHARS sSerialChars = new NativeStructs.SERIAL_CHARS();
                    IntPtr psSerialChars = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialChars));
                    Marshal.StructureToPtr(sSerialChars, psSerialChars, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_CHARS, IntPtr.Zero, 0, psSerialChars,
                        Marshal.SizeOf(sSerialChars), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialChars, sSerialChars);
                    Marshal.FreeHGlobal(psSerialChars);

                    // Get Serial Handflow
                    NativeStructs.SERIAL_HANDFLOW sSerialHandFlow = new NativeStructs.SERIAL_HANDFLOW();
                    IntPtr psSerialHandFlow = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialHandFlow));
                    Marshal.StructureToPtr(sSerialHandFlow, psSerialHandFlow, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_HANDFLOW, IntPtr.Zero, 0, psSerialHandFlow,
                        Marshal.SizeOf(sSerialHandFlow), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialHandFlow, sSerialHandFlow);
                    Marshal.FreeHGlobal(psSerialHandFlow);

                    // Get Serial Baudrate
                    sSerialBaudRate = new NativeStructs.SERIAL_BAUD_RATE();
                    psSerialBaudRate = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialBaudRate));
                    Marshal.StructureToPtr(sSerialBaudRate, psSerialBaudRate, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_BAUD_RATE, IntPtr.Zero, 0, psSerialBaudRate,
                        Marshal.SizeOf(sSerialBaudRate), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialBaudRate, sSerialBaudRate);
                    Marshal.FreeHGlobal(psSerialBaudRate);

                    // Get Serial Line control
                    sSerialLineControl = new NativeStructs.SERIAL_LINE_CONTROL();
                    psSerialLineControl = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialLineControl));
                    Marshal.StructureToPtr(sSerialLineControl, psSerialLineControl, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_LINE_CONTROL, IntPtr.Zero, 0, psSerialLineControl,
                        Marshal.SizeOf(sSerialLineControl), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialLineControl, sSerialLineControl);
                    Marshal.FreeHGlobal(psSerialLineControl);

                    // Get Serial CHARS
                    sSerialChars = new NativeStructs.SERIAL_CHARS();
                    psSerialChars = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialChars));
                    Marshal.StructureToPtr(sSerialChars, psSerialChars, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_CHARS, IntPtr.Zero, 0, psSerialChars,
                        Marshal.SizeOf(sSerialChars), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialChars, sSerialChars);
                    Marshal.FreeHGlobal(psSerialChars);

                    // Get Serial Handflow
                    sSerialHandFlow = new NativeStructs.SERIAL_HANDFLOW();
                    psSerialHandFlow = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialHandFlow));
                    Marshal.StructureToPtr(sSerialHandFlow, psSerialHandFlow, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_GET_HANDFLOW, IntPtr.Zero, 0, psSerialHandFlow,
                        Marshal.SizeOf(sSerialHandFlow), out unBytesReturned, IntPtr.Zero);
                    Marshal.PtrToStructure(psSerialHandFlow, sSerialHandFlow);
                    Marshal.FreeHGlobal(psSerialHandFlow);

                    // Set Serial BaudRate
                    sSerialBaudRate = new NativeStructs.SERIAL_BAUD_RATE();
                    sSerialBaudRate.BaudRate = 300;
                    psSerialBaudRate = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialBaudRate));
                    Marshal.StructureToPtr(sSerialBaudRate, psSerialBaudRate, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_BAUD_RATE, psSerialBaudRate,
                        Marshal.SizeOf(sSerialBaudRate), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialBaudRate);

                    // Set Serial RTS
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_RTS, IntPtr.Zero, 0,
                        IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);

                    // Set Serial DTR
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_DTR, IntPtr.Zero, 0,
                        IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);

                    // Set Serial Line control
                    sSerialLineControl = new NativeStructs.SERIAL_LINE_CONTROL();
                    sSerialLineControl.StopBits = 0x00;
                    sSerialLineControl.Parity = 0x00;
                    sSerialLineControl.WordLength = 0x08;
                    psSerialLineControl = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialLineControl));
                    Marshal.StructureToPtr(sSerialLineControl, psSerialLineControl, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_LINE_CONTROL,
                        psSerialLineControl, Marshal.SizeOf(sSerialLineControl), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialLineControl);

                    // Set Serial CHARS
                    sSerialChars = new NativeStructs.SERIAL_CHARS();
                    sSerialChars.EofChar = 0x00;
                    sSerialChars.ErrorChar = 0x00;
                    sSerialChars.BreakChar = 0x00;
                    sSerialChars.EventChar = 0x00;
                    sSerialChars.XonChar = 0x11;
                    sSerialChars.XoffChar = 0x13;
                    psSerialChars = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialChars));
                    Marshal.StructureToPtr(sSerialChars, psSerialChars, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_CHARS, psSerialChars,
                        Marshal.SizeOf(sSerialChars), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialChars);

                    // Set Serial Handflow
                    sSerialHandFlow = new NativeStructs.SERIAL_HANDFLOW();
                    sSerialHandFlow.ControlHandShake = 0x00000001;
                    sSerialHandFlow.FlowReplace = 0x00000040;
                    sSerialHandFlow.XonLimit = 0x000086C0;
                    sSerialHandFlow.XoffLimit = 0x000021B0;
                    psSerialHandFlow = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialHandFlow));
                    Marshal.StructureToPtr(sSerialHandFlow, psSerialHandFlow, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_HANDFLOW, psSerialHandFlow,
                        Marshal.SizeOf(sSerialHandFlow), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
                    Marshal.FreeHGlobal(psSerialHandFlow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }

            return bResult;
        }

        /* 
         * Function Name: ReadConfigurationData
         * Description: This function reads the configuration data from CH340B's EEPROM and returns the data to the caller
         * Arguments: 
         * int nSelectedDeviceIndex - index of the selected device - this denotes the index in the device list if 
         * there are multiple devices found. If there is only one device, then its value would be 0
         * ref CH340BConfigurationData oCH340BConfigurationData - reference of the object of type CH340BConfigurationData
         * on returning this function, this oCH340BConfigurationData will have the actual configuration data read from the
         * chip.
         * Return: boolean value - true if the read function finishes successfully, else false
         */
        internal bool ReadConfigurationData(int nSelectedDeviceIndex, ref CH340BConfigurationData oCH340BConfigurationData)
        {
            bool bResult = false;

            try
            {
                // Open the device handle
                pHandle = NativeMethods.CreateFile(aoDevInfo[nSelectedDeviceIndex].szComportNameForHandle, Constants.GENERIC_READ | Constants.GENERIC_WRITE,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, IntPtr.Zero);
                if (pHandle == IntPtr.Zero || pHandle == Constants.INVALID_HANDLE_VALUE)
                {
                    MessageBox.Show("Can't open device for reading configuration data.", "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                        MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    return bResult;
                }
                // Initialize the vendor interface of the CH340B driver
                InitializeCh340BVendorInterface(pHandle);
                // Read USB Vendor ID
                // Read the E2PROM location 0x04 and 0x05
                ushort ushVID = 0x0000;
                ushort ushVIDLSB = 0x0000;
                ushort ushVIDMSB = 0x0000;
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x04, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                ushVIDLSB = abyReadData[0];
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x05, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                ushVIDMSB = abyReadData[0];
                ushVID = (ushort)((ushVIDMSB << 8) | ushVIDLSB);
                oCH340BConfigurationData.ushVID = ushVID;

                // Read USB Product ID
                // Read the E2PROM location 0x06 and 0x07
                ushort ushPID = 0x0000;
                ushort ushPIDLSB = 0x0000;
                ushort ushPIDMSB = 0x0000;
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x06, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                ushPIDLSB = abyReadData[0];
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x07, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                ushPIDMSB = abyReadData[0];
                ushPID = (ushort)((ushPIDMSB << 8) | ushPIDLSB);
                oCH340BConfigurationData.ushPID = ushPID;

                // Read USB Product String
                // Read the E2PROM location 0x1A
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x1A, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                byte byUnicodeStringLength = abyReadData[0];

                // Read the E2PROM location 0x1B
                abyWriteData = new byte[4] { 0x40, 0xA1, 0x1B, 0x00 };
                bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                int nUsbProductStringIndentifier = abyReadData[0];

                // Read the E2PROM location 0x1C to 0x3F
                byte i = 0x1C;
                byte[] abyUnicodeProductString = new byte[36];
                int nIndex = 0;
                for (; i <= 0x3F; i++)
                {
                    abyWriteData = new byte[4] { 0x40, 0xA1, i, 0x00 };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                    bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                    abyUnicodeProductString[nIndex++] = abyReadData[0];
                }

                // Perform the conversion from one encoding to the other.
                byte[] abyAsciiProductString = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, abyUnicodeProductString, 0, byUnicodeStringLength - 2);
                // Convert the new byte[] into a char[] and then into a string.
                // This is a slightly different approach to converting to illustrate
                // the use of GetCharCount/GetChars.
                char[] asciiChars = new char[Encoding.UTF8.GetCharCount(abyAsciiProductString, 0, abyAsciiProductString.Length)];
                Encoding.UTF8.GetChars(abyAsciiProductString, 0, abyAsciiProductString.Length, asciiChars, 0);
                string asciiProductString = new string(asciiChars);
                oCH340BConfigurationData.szProductString = asciiProductString;

                // Read USB Serial Number
                // Read EEPROM location 0x10 to 0x17
                string szSerialNumber = "";
                i = 0x10;
                byte[] abyAsciiSerialNumberString = new byte[8];
                nIndex = 0;
                for (; i <= 0x17; i++)  // 0x17 is the last EEPROM location to access
                {
                    abyWriteData = new byte[4] { 0x40, 0xA1, i, 0x00 };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                    bResult = NativeMethods.ReadFile(pHandle, abyReadData, 1, ref unBytesReturned, IntPtr.Zero);
                    abyAsciiSerialNumberString[nIndex++] = abyReadData[0];
                }
                if (abyAsciiSerialNumberString[0] > 0x21 && abyAsciiSerialNumberString[0] < 0x7F)
                {
                    asciiChars = new char[Encoding.ASCII.GetCharCount(abyAsciiSerialNumberString, 0, abyAsciiSerialNumberString.Length)];
                    Encoding.ASCII.GetChars(abyAsciiSerialNumberString, 0, abyAsciiSerialNumberString.Length, asciiChars, 0);
                    szSerialNumber = new string(asciiChars);
                }
                oCH340BConfigurationData.szSerialNumber = szSerialNumber;
                bResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            try
            {
                if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
                {
                    NativeMethods.CloseHandle(pHandle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            return bResult;
        }

        /* 
         * Function Name: WriteConfigurationData
         * Description: This function writes the configuration data to CH340B's EEPROM
         * Arguments: 
         * int nSelectedDeviceIndex - index of the selected device - this denotes the index in the device list if 
         * there are multiple devices found. If there is only one device, then its value would be 0
         * CH340BConfigurationData oCH340BConfigurationData - reference of the object of type CH340BConfigurationData
         * this oCH340BConfigurationData will have the actual configuration data that is filled by the caller.
         * Return: boolean value - true if the write function finishes successfully, else false
         */
        internal bool WriteConfigurationData(int nSelectedDeviceIndex, CH340BConfigurationData oCH340BConfigurationData)
        {
            bool bResult = false;

            do
            {
                try
                {
                    ushort ushVID = oCH340BConfigurationData.ushVID;
                    ushort ushPID = oCH340BConfigurationData.ushPID;
                    string szSerialNumber = oCH340BConfigurationData.szSerialNumber;
                    string szProductString = oCH340BConfigurationData.szProductString;

                    // Open device handle
                    pHandle = NativeMethods.CreateFile(aoDevInfo[nSelectedDeviceIndex].szComportNameForHandle, Constants.GENERIC_READ | Constants.GENERIC_WRITE,
                        Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, IntPtr.Zero);
                    if (pHandle == IntPtr.Zero || pHandle == Constants.INVALID_HANDLE_VALUE)
                    {
                        MessageBox.Show("Can't open device for writing configuration data.", "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                            MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        break;
                    }
                    InitializeCh340BVendorInterface(pHandle);
                    abyWriteData = new byte[4] { 0x40, 0xA0, 0x00, 0x5B };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                    // all the values are now validated

                    // Write VID to EEPROM 0x04 (LSB) and 0x05 (MSB)
                    Byte byVIDLSB = (Byte)(ushVID & 0xFF);
                    Byte byVIDMSB = (Byte)(ushVID >> 8 & 0xFF);

                    abyWriteData = new byte[4] { 0x40, 0xA0, 0x04, byVIDLSB };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);

                    abyWriteData = new byte[4] { 0x40, 0xA0, 0x05, byVIDMSB };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);

                    // Write PID to EEPROM 0x06 (LSB) and 0x07 (MSB)
                    Byte byPIDLSB = (Byte)(ushPID & 0xFF);
                    Byte byPIDMSB = (Byte)(ushPID >> 8 & 0xFF);

                    abyWriteData = new byte[4] { 0x40, 0xA0, 0x06, byPIDLSB };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);

                    abyWriteData = new byte[4] { 0x40, 0xA0, 0x07, byPIDMSB };
                    bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);

                    // Write Serial number to EEPROM
                    if (string.IsNullOrEmpty(szSerialNumber) == false)
                    {
                        // Valid Serail number, write it to EEPROM location 0x10 to 0x17
                        byte[] abyAsciiSerialNumber = Encoding.ASCII.GetBytes(szSerialNumber);

                        // form a 8 byte array
                        int nSerialNumberLength = abyAsciiSerialNumber.Length;

                        if (nSerialNumberLength > 8)
                        {
                            nSerialNumberLength = 8;
                        }

                        byte[] abySerialNumber = new byte[8];
                        Array.Copy(abyAsciiSerialNumber, abySerialNumber, nSerialNumberLength);
                        int nIndex = 0;
                        for (byte i = 0x10; i <= 0x17; i++)
                        {
                            abyWriteData = new byte[4] { 0x40, 0xA0, i, abySerialNumber[nIndex++] };
                            bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                        }
                    }
                    else
                    {
                        // Serial number is empty
                        // Clear the Serial number
                        byte[] abySerialNumber = new byte[8];
                        Array.Clear(abySerialNumber, 0, abySerialNumber.Length);
                        int nIndex = 0;
                        for (byte i = 0x10; i <= 0x17; i++)
                        {
                            abyWriteData = new byte[4] { 0x40, 0xA0, i, abySerialNumber[nIndex++] };
                            bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                        }
                    }

                    // Write Product String to device
                    // Prepare the array of bytes with Unicode byte values of the string and write the array to EEPROM location 0x1A to 0x3F
                    if (string.IsNullOrEmpty(szProductString) == false)
                    {
                        byte[] abyUnicodeProdString = Encoding.Unicode.GetBytes(szProductString);

                        // form a 38 byte array
                        byte[] abyProdStringBuffer = new byte[38];
                        abyProdStringBuffer[0] = (byte)(abyUnicodeProdString.Length + 2);
                        abyProdStringBuffer[1] = 0x03;

                        Array.Copy(abyUnicodeProdString, 0, abyProdStringBuffer, 2, abyUnicodeProdString.Length);

                        int nIndex = 0;
                        for (byte i = 0x1A; i <= 0x3F; i++)
                        {
                            abyWriteData = new byte[4] { 0x40, 0xA0, i, abyProdStringBuffer[nIndex++] };
                            bResult = NativeMethods.WriteFile(pHandle, abyWriteData, (uint)abyWriteData.Length, ref unBytesReturned, IntPtr.Zero);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                        MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }

            } while (false);
            try
            {
                // Close the device handle
                if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
                {
                    NativeMethods.CloseHandle(pHandle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            return bResult;
        }
    }
}
