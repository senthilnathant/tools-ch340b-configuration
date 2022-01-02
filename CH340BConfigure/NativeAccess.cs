// *********************************************************************************************************
//
//	   Project      : WCH CH340B Configuration Utility
//	   FileName     : NativeAccess.cs
//	   Author       : SENTHILNATHAN THANGAVEL
//     Co-Author(s) : 
//	   Created      : ‎02 January, ‎2022
//
// *********************************************************************************************************
//
// Module Description
//
// This module has classes that contain managed version of Win32 native structs, constants, native methods
// Here C# .Net Framework's Platform Invoke (P/Invoke) interoperability service is used to call the
// native unmanaged Win32 APIs.
// *********************************************************************************************************
//
// History
//
// Date			        Version		Author		                Changes
//
// ‎02 January, ‎2022	    1.0.0		SENTHILNATHAN THANGAVEL		Initial version
//
// *********************************************************************************************************
using System;
using System.Runtime.InteropServices;

namespace CH340BConfigure
{
    public class NativeAccess
    {
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
    }
}
