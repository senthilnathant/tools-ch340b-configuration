// *********************************************************************************************************
//
//	   Project      : WCH CH340B Configuration Utility
//	   FileName     : Program.cs
//	   Author       : SENTHILNATHAN THANGAVEL, INDEPENDENT DEVELOPER
//     Co-Author(s) : 
//	   Created      : ‎20 December, ‎2021
//
// *********************************************************************************************************
//
// Module Description
//
// IDE generated code - main entry point of the application
// *********************************************************************************************************
//
// History
//
// Date			        Version		Author		                Changes
//
// 20 December, ‎2021	1.0.0		SENTHILNATHAN THANGAVEL		Initial version
//
// *********************************************************************************************************
using System;
using System.Windows.Forms;

namespace CH340BConfigure
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UiForm());
        }
    }
}
