// *********************************************************************************************************
//
//	   Project      : WCH CH340B Configuration Utility
//	   FileName     : UiForm.cs
//	   Author       : SENTHILNATHAN THANGAVEL, INDEPENDENT DEVELOPER
//     Co-Author(s) : 
//	   Created      : ‎20 December, ‎2021
//
// *********************************************************************************************************
//
// Module Description
//
// This is the main user interface form which is displayed when the 
// executble is run. This has the ui button controls and event handlers, input / output text box controls.
// This module will detect the CH340B devices connected to the PC and list the detected devices on its ui
// combo box control. Based on the Read/Write function selected this module would send the Read / Write
// request to the selected device in the device list.
// *********************************************************************************************************
//
// History
//
// Date			        Version		Author		                Changes
//
// 20 December, ‎2021	1.0.0		SENTHILNATHAN THANGAVEL		Initial version
// 02 January, ‎2022	    1.0.1		SENTHILNATHAN THANGAVEL		Code restructured to provide good modularity
//
// *********************************************************************************************************
using System;
using System.Text;
using System.Windows.Forms;

namespace CH340BConfigure
{
    public partial class UiForm : Form
    {
        // Number of CH340B devices connected to the PC
        private int nNumberOfDevices = 0;
        // Device index tracking when a new device is selected in the device list combo box
        private int nSelectedDeviceIndex = 0;
        // CH340BDeviceAccess object - this will have the device information, device detection function,
        // configuration data read/write functions
        private CH340BDeviceAccess oCH340BDeviceAccess = new CH340BDeviceAccess();

        public UiForm()
        {
            // IDE generated
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
            nNumberOfDevices = oCH340BDeviceAccess.FindDevices();
            if (nNumberOfDevices > 0)
            {
               // Add the CH340B devices detected to the combobox
                for (int i = 0; i < nNumberOfDevices; i++)
                {
                    comboBoxDeviceList.Items.Insert(i, oCH340BDeviceAccess.aoDevInfo[i].szDeviceName);
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

            buttonRead.Enabled = false;

            try
            {
                labelStatus.Text = "";
                CH340BConfigurationData oCH340BConfigurationData = new CH340BConfigurationData();
                bResult = oCH340BDeviceAccess.ReadConfigurationData(nSelectedDeviceIndex, ref oCH340BConfigurationData);
                // Display the configuration data to the UI 
                if (bResult)
                {
                    textBoxVid.Text = oCH340BConfigurationData.ushVID.ToString("X4");
                    textBoxPid.Text = oCH340BConfigurationData.ushPID.ToString("X4");
                    textBoxProductString.Text = oCH340BConfigurationData.szProductString;
                    textBoxSerialNumber.Text = oCH340BConfigurationData.szSerialNumber;
                    // Update the Read status
                    labelStatus.Text = "Status: Read Success";
                }
                else
                {
                    // Update the Read status
                    labelStatus.Text = "Status: Read Failed";
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message, "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
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
                catch (Exception)
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
                catch (Exception)
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
                catch (Exception)
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
                catch (Exception)
                {
                    MessageBox.Show("Enter valid string value for Product String.", "CH340B Configuration Utlity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    break;
                }

                // Write the configuration data to CH340B
                CH340BConfigurationData oCH340BConfigurationData = new CH340BConfigurationData();
                oCH340BConfigurationData.ushVID = ushVID;
                oCH340BConfigurationData.ushPID = ushPID;
                oCH340BConfigurationData.szProductString = szProductString;
                oCH340BConfigurationData.szSerialNumber = szSerialNumber;
                bResult = oCH340BDeviceAccess.WriteConfigurationData(nSelectedDeviceIndex, oCH340BConfigurationData);

                // Update the Write status
                if (bResult)
                {
                    labelStatus.Text = "Status: Write Success";
                }
                else
                {
                    labelStatus.Text = "Status: Write Failed";
                }
            } while (false);    

            buttonWrite.Enabled = true;
        }
    }

    

    
}
