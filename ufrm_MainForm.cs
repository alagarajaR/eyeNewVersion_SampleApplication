using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using peak;

namespace SampleApplication
{
    public partial class ufrm_MainForm : Form
    {
        private BackEnd backEnd;
        private BackgroundWorker backgroundWorker1;
        private bool hasError;

        public ufrm_MainForm()
        {
            InitializeComponent();
            backEnd = new BackEnd();

            FormClosing += ufrm_MainForm_FormClosing;

            backEnd.ImageReceived += backEnd_ImageReceived;
            backEnd.CounterChanged += backEnd_CounterChanged;
            backEnd.MessageBoxTrigger += backEnd_MessageBoxTrigger;

        }


        private void backEnd_ImageReceived(object sender, Bitmap image)
        {
            try
            {
                var previousImage = pictureBox.Image;

                pictureBox.Image = image;

                // Manage memory usage by disposing the previous image
                if (previousImage != null)
                {
                    previousImage.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("--- [FormWindow] Exception: " + e.Message);
                backEnd_MessageBoxTrigger(this, "Exception", e.Message);
            }
        }

        private void backEnd_MessageBoxTrigger(object sender, String messageTitle, String messageText)
        {
            MessageBox.Show(messageText, messageTitle);
        }

        private void backEnd_CounterChanged(object sender, uint frameCounter, uint errorCounter)
        {
            if (counterLabel.InvokeRequired)
            {
                counterLabel.BeginInvoke((MethodInvoker)delegate { counterLabel.Text = "Frames acquired: " + frameCounter + ", errors: " + errorCounter; });
            }
        }


        private void ufrm_MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //peak.Library.Close();
        }

        private void btn_OpenCamera_Click(object sender, EventArgs e)
        {
            if (backEnd.start())
            {
                hasError = false;
            }
            else
            {
                hasError = true;
            }

            //int iRetval = 0;
            //string errorString = string.Empty;
            //OpenCamera(ref iRetval,ref errorString);
            //switch(iRetval)
            //{
            //    case -1:
            //        MessageBox.Show("No device found. Exiting program.");
            //        break;
            //    case -2:
            //        MessageBox.Show("Error Opening Device: " + errorString);
            //        break;
            //    default:
            //        MessageBox.Show("Camera Openeded Successfully !!!");
            //        break;
            //}
        }

        //private void OpenCamera(ref int retVal,ref string errorString)
        //{
        //    peak.Library.Initialize();

        //    // create a DeviceManager object
        //    var deviceManager = peak.DeviceManager.Instance();

        //    try
        //    {
        //        // update the DeviceManager
        //        deviceManager.Update();

        //        // exit program if no device was found
        //        if (!deviceManager.Devices().Any())
        //        {
        //            peak.Library.Close();
        //            retVal = - 1;
        //        }

        //        // open the first device
        //        var device = deviceManager.Devices()[0].OpenDevice(peak.core.DeviceAccessType.Control);
        //    }
        //    catch (Exception ex)
        //    {
        //        errorString = ex.Message;
        //        peak.Library.Close();
        //        retVal = -2;
        //    }

        //    // close library
        //    peak.Library.Close();
        //}

        private void ufrm_MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (backEnd.start())
                {
                    hasError = false;
                }
                else
                {
                    hasError = true;
                }
            }
            catch(Exception ex)
            {
                backEnd_MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        public bool HasError()
        {
            return hasError;
        }

        private void ufrm_MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("--- [FormWindow] Closing");
            if (backEnd.IsActive())
                backEnd.Stop();
        }

        private void ufrm_MainForm_Shown(object sender, EventArgs e)
        {

        }
    }
}
