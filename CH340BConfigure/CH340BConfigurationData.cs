// *********************************************************************************************************
//
//	   Project      : WCH CH340B Configuration Utility
//	   FileName     : CH340BConfigurationData.cs
//	   Author       : SENTHILNATHAN THANGAVEL, INDEPENDENT DEVELOPER
//     Co-Author(s) : 
//	   Created      : ‎02 January, ‎2022
//
// *********************************************************************************************************
//
// Module Description
//
// This class declares the Configuration data such as VID, PID, Product String and Serial number of CH340B
// *********************************************************************************************************
//
// History
//
// Date			            Version		Author		                Changes
//
// ‎02 January, ‎2022   		1.0.0		SENTHILNATHAN THANGAVEL		Initial version
//
// *********************************************************************************************************
namespace CH340BConfigure
{
    public class CH340BConfigurationData
    {
        public ushort ushVID;
        public ushort ushPID;
        public string szProductString;
        public string szSerialNumber;
    }
}
