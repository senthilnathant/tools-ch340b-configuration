using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CH340BConfigure
{
    public partial class Form1 : Form
    {
        // Allocate separate buffers for Read and Write functions
        Byte[] abyReadData = new byte[1024];
        Byte[] abyWriteData = new byte[1024];
        // Allocate a byte array buffer for using in the DeviceIoControl calls
        Byte[] abyInputBuffer = null;
        uint unBytesReturned = 0;
        // Device Handle
        IntPtr pHandle = IntPtr.Zero;
        IntPtr pspDeviceInterfaceData = IntPtr.Zero;
        IntPtr pspDeviceInterfaceDetailData = IntPtr.Zero;
        // Device Info object array that has device's user friendly name, actual COM port name
        // This application supports multiple CH340B devices connected to the same PC's USB ports - up to 10 number of devices
        DeviceInfo[] aoDevInfo = DeviceInfo.NewInitArray(10);
        int nNumberOfDevices = 0;
        // Device index tracking when a new device is selected in the device list combo box
        int nSelectedDeviceIndex = 0;

        public Form1()
        {
            InitializeComponent();
            // This closing handler does nothing now
            this.FormClosing += Form1_FormClosing;
            // Call the SearchAndListCh340BDevices - i.e look for devices and list them
            buttonUpdateDevList_Click(null, null);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
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
                uintError = Constants.NO_ERROR;
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

            }
            
            return bResult;
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
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_RTS, IntPtr.Zero, 0, IntPtr.Zero, 
                        0, out unBytesReturned, IntPtr.Zero);

                    // Set Serial DTR
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_DTR, IntPtr.Zero, 0, IntPtr.Zero, 
                        0, out unBytesReturned, IntPtr.Zero);

                    // Set Serial Line control
                    sSerialLineControl = new NativeStructs.SERIAL_LINE_CONTROL();
                    sSerialLineControl.StopBits = 0x00;
                    sSerialLineControl.Parity = 0x00;
                    sSerialLineControl.WordLength = 0x08;
                    psSerialLineControl = Marshal.AllocHGlobal(Marshal.SizeOf(sSerialLineControl));
                    Marshal.StructureToPtr(sSerialLineControl, psSerialLineControl, false);
                    bResult = NativeMethods.DeviceIoControl(pHandle, Constants.IOCTL_SERIAL_SET_LINE_CONTROL, psSerialLineControl, 
                        Marshal.SizeOf(sSerialLineControl), IntPtr.Zero, 0, out unBytesReturned, IntPtr.Zero);
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

                    // The magic value 0x43485523 is returned in the unsigned integer ProvSpec2 of Serial Comm Prop structure by the CH340B driver
                    if (sSerialCompProp.ProvSpec2 == 0x43485523)
                    {
                        // Device is CH340B
                        bDeviceIsCh340B = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            // Close the device handle
            if (pHandle != IntPtr.Zero && pHandle != Constants.INVALID_HANDLE_VALUE)
            {
                NativeMethods.CloseHandle(pHandle);
            }
            return bDeviceIsCh340B;
        }

        /* 
         * Function Name: buttonUpdateDevList_Click
         * Description: UpdateDevList button click event handler
         */
        private void buttonUpdateDevList_Click(object sender, EventArgs e)
        {
            // Init the ui contorls state
            buttonUpdateDevList.Enabled = false;
            groupBoxConfigControls.Enabled = false;
            // Look for the CH340B devices and list them on the device list combo box
            SearchAndListCh340BDevices();
            buttonUpdateDevList.Enabled = true;
            if (nNumberOfDevices > 0)
            {
                groupBoxConfigControls.Enabled = true;                
            }
            else
            {
                groupBoxConfigControls.Enabled = false;
            }
            labelStatus.Text = "";
        }

        /* 
         * Function Name: SearchAndListCh340BDevices
         * Description: This function detects the CH340B devices and list them on the combo boxes
         */
        private void SearchAndListCh340BDevices()
        {
            // If there is already a list of devices, free the list of device and resources allocated
            comboBoxDeviceList.Items.Clear();
            // Look for the CH340B devices connected to the System
            bool bResult = GetDevices(ref aoDevInfo, ref nNumberOfDevices);
            if (nNumberOfDevices > 0)
            {
               // Add the CH340B devices detected to the combobox
                for (int i = 0; i < nNumberOfDevices; i++)
                {
                    comboBoxDeviceList.Items.Insert(i, aoDevInfo[i].szDeviceName);
                }
                comboBoxDeviceList.SelectedIndex = 0;
                nSelectedDeviceIndex = 0;
            }
        }

        /* 
         * Function Name: comboBoxDeviceList_SelectedIndexChanged
         * Description: This function is the Combobox Device List index changed handler
         */
        private void comboBoxDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Store the selected index to the device index variable
            nSelectedDeviceIndex = comboBoxDeviceList.SelectedIndex;
        }

        /* 
        * Function Name: buttonRead_Click
        * Description: This function is buttonRead click event handler
        */
        private void buttonRead_Click(object sender, EventArgs e)
        {
            bool bResult = false;
            buttonRead.Enabled = false;
            textBoxVid.Text = "";
            textBoxPid.Text = "";
            textBoxProductString.Text = "";
            textBoxSerialNumber.Text = "";
            try
            {
                labelStatus.Text = "";
                // Open the device handle
                pHandle = NativeMethods.CreateFile(aoDevInfo[nSelectedDeviceIndex].szComportNameForHandle, Constants.GENERIC_READ | Constants.GENERIC_WRITE,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, IntPtr.Zero);
                if (pHandle == IntPtr.Zero || pHandle == Constants.INVALID_HANDLE_VALUE)
                {
                    MessageBox.Show("Can't open device for reading configuration data.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    buttonRead.Enabled = true;
                    return;
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
                textBoxVid.Text = ushVID.ToString("X4");

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
                textBoxPid.Text = ushPID.ToString("X4");

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
                textBoxProductString.Text = asciiProductString.ToString();

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
                textBoxSerialNumber.Text = szSerialNumber;
            }
            catch (Exception ex)
            {

            }
            // Update the Read status
            if (bResult)
            {
                labelStatus.Text = "Status: Read Success";
            }
            else
            {
                labelStatus.Text = "Status: Read Failed";
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

            }
            buttonRead.Enabled = true;
        }

        /* 
        * Function Name: buttonWrite_Click
        * Description: This function is buttonWrite click event handler
        */
        private void buttonWrite_Click(object sender, EventArgs e)
        {
            ushort ushVID = 0x0000;
            ushort ushPID = 0x0000;
            string szProductString = "";
            string szSerialNumber = "";
            bool bResult = false;
            labelStatus.Text = "";
            buttonWrite.Enabled = false;
            do
            {
                // Validate VID
                try
                {
                    string szVID = textBoxVid.Text;
                    ushVID = Convert.ToUInt16(szVID, 16);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Enter valid value for Vendor ID in hex.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    break;
                }

                // Validate PID
                try
                {
                    string szPID = textBoxPid.Text;
                    ushPID = Convert.ToUInt16(szPID, 16);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Enter valid value for Product ID in hex.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    break;
                }

                // Validate Product string
                try
                {
                    szProductString = textBoxProductString.Text;
                    if (string.IsNullOrEmpty(szProductString))
                    {
                        MessageBox.Show("Enter valid string value for Product String.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Enter valid string value for Product String.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    break;
                }

                // Validate Serial Number
                try
                {
                    szSerialNumber = textBoxSerialNumber.Text;
                    if (string.IsNullOrEmpty(szSerialNumber) == false)
                    {
                        byte[] abySerialNumber = Encoding.ASCII.GetBytes(szSerialNumber);

                        for (int i = 0; i < abySerialNumber.Length; i++)
                        {
                            if ((abySerialNumber[i] >= 0x30 && abySerialNumber[i] <= 0x39) || (abySerialNumber[i] >= 0x41 && abySerialNumber[i] <= 0x5A) || (abySerialNumber[i] >= 0x61 && abySerialNumber[i] <= 0x7A))
                            {
                                // Valid serial number string
                            }
                            else
                            {
                                MessageBox.Show("Enter valid string value for Serial Number.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Enter valid string value for Product String.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    break;
                }

                try
                {
                    // Open device handle
                    pHandle = NativeMethods.CreateFile(aoDevInfo[nSelectedDeviceIndex].szComportNameForHandle, Constants.GENERIC_READ | Constants.GENERIC_WRITE,
                        Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, IntPtr.Zero);
                    if (pHandle == IntPtr.Zero || pHandle == Constants.INVALID_HANDLE_VALUE)
                    {
                        MessageBox.Show("Can't open device for writing configuration data.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
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

                }
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

                }

            } while (false);

            // Update the Write status
            if (bResult)
            {
                labelStatus.Text = "Status: Write Success";
            }
            else
            {
                labelStatus.Text = "Status: Write Failed";
            }

            buttonWrite.Enabled = true;
        }
    }

    internal class NativeStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal class SP_DEVINFO_DATA
        {
            public Int32 cbSize;
            public Guid ClassGuid;
            public Int32 DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public Int32 cbSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
            public byte[] DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SP_DEVICE_INTERFACE_DATA
        {
            public Int32 cbSize;
            public System.Guid InterfaceClassGuid;
            public Int32 Flags;
            public UIntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class OVERLAPPED
        {
            public IntPtr InternalLow;
            public IntPtr InternalHigh;
            public long Offset;
            public IntPtr hEvent;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_COMMPROP
        {
            public UInt16 PacketLength;
            public UInt16 PacketVersion;
            public UInt32 ServiceMask;
            public UInt32 Reserved1;
            public UInt32 MaxTxQueue;
            public UInt32 MaxRxQueue;
            public UInt32 MaxBaud;
            public UInt32 ProvSubType;
            public UInt32 ProvCapabilities;
            public UInt32 SettableParams;
            public UInt32 SettableBaud;
            public UInt16 SettableData;
            public UInt16 SettableStopParity;
            public UInt32 CurrentTxQueue;
            public UInt32 CurrentRxQueue;
            public UInt32 ProvSpec1;
            public UInt32 ProvSpec2;
            public IntPtr ProvChar;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_QUEUE_SIZE
        {
            public UInt32 InSize;
            public UInt32 OutSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_TIMEOUTS
        {
            public UInt32 ReadIntervalTimeout;
            public UInt32 ReadTotalTimeoutMultiplier;
            public UInt32 ReadTotalTimeoutConstant;
            public UInt32 WriteTotalTimeoutMultiplier;
            public UInt32 WriteTotalTimeoutConstant;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_BAUD_RATE
        {
            public UInt32 BaudRate;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_LINE_CONTROL
        {
            public Byte StopBits;
            public Byte Parity;
            public Byte WordLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_CHARS
        {
            public Byte EofChar;
            public Byte ErrorChar;
            public Byte BreakChar;
            public Byte EventChar;
            public Byte XonChar;
            public Byte XoffChar;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class SERIAL_HANDFLOW
        {
            public UInt32 ControlHandShake;
            public UInt32 FlowReplace;
            public Int32 XonLimit;
            public Int32 XoffLimit;
        }
    }

    internal class Constants
    {
        // Define constants
        // internal const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1); 

        internal const uint ERROR_INVALID_HANDLE = 0x00000006;
        internal const uint ERROR_INSUFFICIENT_BUFFER = 122;

        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;

        internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const uint OPEN_EXISTING = 0x00000003;

        internal const uint NO_ERROR = 0x00000000;
        internal const uint STATUS_SUCCESS = 0x00000000;

        internal const uint DIGCF_DEFAULT = 0x00000001;
        internal const uint DIGCF_PRESENT = 0x00000002;
        internal const uint DIGCF_ALLCLASSES = 0x00000004;
        internal const uint DIGCF_PROFILE = 0x00000008;
        internal const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        internal const UInt32 ERROR_IO_PENDING = 997;
        internal const UInt32 INFINITE = 0xFFFFFFFF;
        internal const UInt32 WAIT_ABANDONED = 0x00000080;
        internal const UInt32 WAIT_OBJECT_0 = 0x00000000;
        internal const UInt32 WAIT_TIMEOUT = 0x00000102;

        internal const UInt32 IOCTL_SERIAL_GET_PROPERTIES = 0x1b0074;
        internal const UInt32 IOCTL_SERIAL_SET_QUEUE_SIZE = 0x1b0008;
        internal const UInt32 IOCTL_SERIAL_GET_TIMEOUTS = 0x1b0020;
        internal const UInt32 IOCTL_SERIAL_SET_TIMEOUTS = 0x1b001c;
        internal const UInt32 IOCTL_SERIAL_PURGE = 0x1b004c;
        internal const UInt32 IOCTL_SERIAL_GET_BAUD_RATE = 0x1b0050;
        internal const UInt32 IOCTL_SERIAL_GET_LINE_CONTROL = 0x1b0054;
        internal const UInt32 IOCTL_SERIAL_GET_CHARS = 0x1b0058;
        internal const UInt32 IOCTL_SERIAL_GET_HANDFLOW = 0x1b0060;
        internal const UInt32 IOCTL_SERIAL_SET_BAUD_RATE = 0x1b0004;
        internal const UInt32 IOCTL_SERIAL_SET_RTS = 0x1b0030;
        internal const UInt32 IOCTL_SERIAL_SET_DTR = 0x1b0024;
        internal const UInt32 IOCTL_SERIAL_SET_LINE_CONTROL = 0x1b000c;
        internal const UInt32 IOCTL_SERIAL_SET_CHARS = 0x1b005c;
        internal const UInt32 IOCTL_SERIAL_SET_HANDFLOW = 0x1b0064;

        internal enum SPDRP
        {
            SPDRP_DEVICEDESC = 0,
            SPDRP_HARDWAREID = 0x1,
            SPDRP_COMPATIBLEIDS = 0x2,
            SPDRP_UNUSED0 = 0x3,
            SPDRP_SERVICE = 0x4,
            SPDRP_UNUSED1 = 0x5,
            SPDRP_UNUSED2 = 0x6,
            SPDRP_CLASS = 0x7,
            SPDRP_CLASSGUID = 0x8,
            SPDRP_DRIVER = 0x9,
            SPDRP_CONFIGFLAGS = 0xa,
            SPDRP_MFG = 0xb,
            SPDRP_FRIENDLYNAME = 0xc,
            SPDRP_LOCATION_INFORMATION = 0xd,
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0xe,
            SPDRP_CAPABILITIES = 0xf,
            SPDRP_UI_NUMBER = 0x10,
            SPDRP_UPPERFILTERS = 0x11,
            SPDRP_LOWERFILTERS = 0x12,
            SPDRP_BUSTYPEGUID = 0x13,
            SPDRP_LEGACYBUSTYPE = 0x14,
            SPDRP_BUSNUMBER = 0x15,
            SPDRP_ENUMERATOR_NAME = 0x16,
            SPDRP_SECURITY = 0x17,
            SPDRP_SECURITY_SDS = 0x18,
            SPDRP_DEVTYPE = 0x19,
            SPDRP_EXCLUSIVE = 0x1a,
            SPDRP_CHARACTERISTICS = 0x1b,
            SPDRP_ADDRESS = 0x1c,
            SPDRP_UI_NUMBER_DESC_FORMAT = 0x1e,
            SPDRP_MAXIMUM_PROPERTY = 0x1f

        }
    }

    internal class NativeMethods
    {
        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern void HidD_GetHidGuid(ref System.Guid HidGuid);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean HidD_SetOutputReport(IntPtr HidDeviceObject, Byte[] lpReportBuffer,
            Int32 ReportBufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean HidD_SetFeature(IntPtr HidDeviceObject, Byte[] lpReportBuffer,
             Int32 ReportBufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean HidD_GetFeature(IntPtr HidDeviceObject, [Out] byte[] lpBuffer, Int32 ReportBufferLength);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid,
            IntPtr Enumerator, IntPtr hwndParent, UInt32 Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern Boolean SetupDiEnumDeviceInfo(IntPtr hDevInfo,
            uint uintDeviceID, IntPtr psDeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern Boolean SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet,
            IntPtr psDeviceInfoData, uint Property, ref uint PropertyRegDataType, IntPtr pchBuffer,
            uint uintBufferSize, ref uint untBufferSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid,
            uint MemberIndex, IntPtr psDeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo,
            IntPtr pspDeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize,
            IntPtr DeviceInfoData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetLastError();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateFile(String lpFileName, UInt32 dwDesiredAccess,
            UInt32 dwShareMode, IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition,
            UInt32 dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean CloseHandle(IntPtr pHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean WriteFile(IntPtr pHandle, Byte[] lpReportBuffer,
            uint unReportBufferSize, ref UInt32 lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern Boolean HidD_GetInputReport(IntPtr hFileHandle, byte[] reportBuffer, uint reportBufferLength);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        internal static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CancelIo(IntPtr hFile);

        [DllImport("kernel32.dll")]
        internal static extern bool SetEvent(IntPtr hEvent);


        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(IntPtr hDevice, uint ioControlCode,
            [MarshalAs(UnmanagedType.LPArray)][In] byte[] inBuffer, int ninBufferSize,
            [MarshalAs(UnmanagedType.LPArray)][Out] byte[] outBuffer, int noutBufferSize,
            out uint bytesReturned, [In] IntPtr overlapped);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(IntPtr hDevice, uint ioControlCode, IntPtr inBuffer, int ninBufferSize,
            IntPtr outBuffer, int noutBufferSize, out uint bytesReturned, [In] IntPtr overlapped);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(IntPtr hDevice, uint ioControlCode, byte[] inBuffer, int ninBufferSize,
            IntPtr outBuffer, int noutBufferSize, out uint bytesReturned, [In] IntPtr overlapped);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(IntPtr hDevice, uint ioControlCode, IntPtr inBuffer, int ninBufferSize,
            byte[] outBuffer, int noutBufferSize, out uint bytesReturned, [In] IntPtr overlapped);

    }

    public class DeviceInfo
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
}
