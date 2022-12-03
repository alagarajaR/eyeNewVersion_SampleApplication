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
using static SampleApplication.Global;

namespace SampleApplication
{
    public partial class ufrm_uEyeInitNew : Form
    {
        private BackEnd backEnd;
        private BackgroundWorker backgroundWorker1;
        private bool hasError;
        private DeviceConfig deviceConfig;

        public string imageLocation = string.Empty;
        public ufrm_uEyeInitNew()
        {
            InitializeComponent();
            backEnd = new BackEnd();

            FormClosing += ufrm_MainForm_FormClosing;

            backEnd.ImageReceived += backEnd_ImageReceived;
            backEnd.CounterChanged += backEnd_CounterChanged;
            backEnd.MessageBoxTrigger += backEnd_MessageBoxTrigger;
        }

        private void ufrm_MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("--- [FormWindow] Closing");
            if (backEnd.IsActive())
                backEnd.Stop();
        }

        private void backEnd_ImageReceived(object sender, Bitmap image)
        {
            try
            {
                var previousImage = DisplayWindow.Image;

                DisplayWindow.Image = image;

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
            //if (frameCount.InvokeRequired)
            //{
            //    frameCount.BeginInvoke((MethodInvoker)delegate { frameCount.Text = "Frames acquired: " + frameCounter + ", errors: " + errorCounter; });
            //}
        }

        private void ufrm_uEyeInitNew_Load(object sender, EventArgs e)
        {
            //splt_Container.Panel2Collapsed = !chkb_CameraSettings.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BUTTON_START_Click(object sender, EventArgs e)
        {
            if (backEnd.start())
            {
                hasError = false;
                BUTTON_START.Enabled = false;
                BUTTON_STOP.Enabled = true;
                BUTTON_SAVE.Enabled = true;
                chkb_CameraSettings.Enabled = true;
            }
            else
            {
                hasError = true;
            }
        }

        private void UpdateControlValues()
        {
            try
            {
                nupd_FrameRate.Value = deviceConfig.frameRate;
                nupd_ExpTime.Value = deviceConfig.exposureTime / 1000;


                // Auto Expsosure Mode
                switch(deviceConfig.exposureAuto)
                {
                    case "Off":
                        rdb_AEOff.Checked = true;
                        break;
                    case "Once":
                        rdb_AEOnce.Checked = true;
                        break;
                    case "Continuous":
                        rdb_AEContinous.Checked = true;
                        break;
                }


                // Auto Expsosure Mode
                switch (deviceConfig.gainAuto)
                {
                    case "Off":
                        rdb_AEOff.Checked = true;
                        break;
                    case "Once":
                        rdb_AEOnce.Checked = true;
                        break;
                    case "Continuous":
                        rdb_AEContinous.Checked = true;
                        break;
                }

                // Color Correction Value

                txt_Gain00.Text = deviceConfig.gainMatrix.gain00.ToString();
                txt_Gain01.Text = deviceConfig.gainMatrix.gain01.ToString();
                txt_Gain02.Text = deviceConfig.gainMatrix.gain02.ToString();

                txt_Gain10.Text = deviceConfig.gainMatrix.gain10.ToString();
                txt_Gain11.Text = deviceConfig.gainMatrix.gain11.ToString();
                txt_Gain12.Text = deviceConfig.gainMatrix.gain12.ToString();

                txt_Gain20.Text = deviceConfig.gainMatrix.gain20.ToString();
                txt_Gain21.Text = deviceConfig.gainMatrix.gain21.ToString();
                txt_Gain22.Text = deviceConfig.gainMatrix.gain22.ToString();

            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Updating Controls" + ex.Message);
            }
        }

        private void ufrm_uEyeInitNew_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("--- [FormWindow] Closing");
            if (backEnd.IsActive())
                backEnd.Stop();
        }

        private void BUTTON_STOP_Click(object sender, EventArgs e)
        {
            /* Stop the camera acquisition */
            Debug.WriteLine("--- Button Closed");
            if (backEnd.IsActive())
                backEnd.Stop();

            /* active keys Strart Live and Stop Live */
            BUTTON_START.Enabled = true;
            BUTTON_STOP.Enabled = false;
            BUTTON_SAVE.Enabled = false;
            chkb_CameraSettings.Enabled = false;
        }

        private void ufrm_uEyeInitNew_Shown(object sender, EventArgs e)
        {
            try
            {
                if (backEnd.start())
                {
                    hasError = false;
                    BUTTON_START.Enabled = false;
                    BUTTON_STOP.Enabled = true;
                    BUTTON_SAVE.Enabled = true;
                    chkb_CameraSettings.Enabled = true;

                    backEnd.GetConfigration(ref deviceConfig);
                    UpdateControlValues();
                }
                else
                {
                    hasError = true;
                    BUTTON_START.Enabled = true;
                    BUTTON_STOP.Enabled = false;
                    BUTTON_SAVE.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                backEnd_MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        // Save Image
        private void BUTTON_SAVE_Click(object sender, EventArgs e)
        {

        }

        private void chkb_CameraSettings_Click(object sender, EventArgs e)
        {
            splt_Container.Panel2Collapsed = !chkb_CameraSettings.Checked;
        }

        private void nupd_FrameRate_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setFrameRate(nupd_FrameRate.Value);
            }

        }

        private void nupd_ExpTime_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setExposureTime(nupd_ExpTime.Value);
            }

        }

        private void cbhkb_AutoGain_CheckedChanged(object sender, EventArgs e)
        {
            nupd_AnalogGain.Enabled = cbhkb_AutoGain.Checked;
            cbhkb_DigitalGain.Enabled = !cbhkb_AutoGain.Checked;
        }

        private void cbhkb_DigitalGain_CheckedChanged(object sender, EventArgs e)
        {
            nupd_DigitalGain.Enabled = cbhkb_DigitalGain.Checked;
            cbhkb_AutoGain.Enabled = !cbhkb_DigitalGain.Checked;
        }

        private void nupd_AnalogGain_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setAnalogGain(nupd_AnalogGain.Value);
            }

        }

        private void nupd_DigitalGain_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setAnalogGain(nupd_DigitalGain.Value);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdb_AEOnce_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AEOnce.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoExpo(rdb_AEOnce.Text);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdb_AEContinous_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AEContinous.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoExpo(rdb_AEContinous.Text);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdb_AEOff_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AEOff.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoExpo(rdb_AEOff.Text);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void rdb_AGOff_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AEOff.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoGain(rdb_AEOff.Text);
                }
            }
        }

        private void rdb_AGOnce_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AGOnce.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoGain(rdb_AGOnce.Text);
                }
            }
        }

        private void rdb_AGContinous_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AGContinous.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoGain(rdb_AGContinous.Text);
                }
            }
        }

        private void nupd_Percentile_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setPercentile(nupd_Percentile.Value);
            }
        }

        private void nupd_Target_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setTarget((int)nupd_Target.Value);
            }
        }

        private void nupd_Tolerence_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setTolrence((int)nupd_Tolerence.Value);
            }
        }

        private void rdb_AWOff_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AWOff.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoWhiteBalance(rdb_AWOff.Text);
                }
            }
        }


        private void rdb_AWOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AWOn.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoWhiteBalance(rdb_AWOn.Text);
                }
            }
        }

        private void rdb_AWOnce_CheckedChanged(object sender, EventArgs e)
        {
            if (rdb_AWOnce.Checked)
            {
                if (backEnd.IsActive())
                {
                    backEnd.setAutoWhiteBalance(rdb_AWOnce.Text);
                }
            }
        }


        private void nud_Red_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setDigitalgRGain(nupd_RGain.Value);
            }
        }

        private void nupd_GGain_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setDigitalgGGain(nupd_GGain.Value);
            }
        }

        private void nupd_BGain_ValueChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setDigitalgBGain(nupd_BGain.Value);
            }
        }

        private void chkb_VFlip_CheckedChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setReverseY(chkb_VFlip.Checked);
            }
        }

        private void chkb_HFlip_CheckedChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setReverseX(chkb_HFlip.Checked);
            }
        }

        private void cmb_ColorCorrection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setColorCorrectionMode(cmb_ColorCorrection.Text);
            }
        }

        private void cmb_ColorMatrix_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                switch (cmb_ColorMatrix.Text)
                {
                    case "HQ":
                        UpdateColorMatrixControls(false);
                        backEnd.setColorCorrectionMatrix(cmb_ColorMatrix.Text);
                        break;
                    case "Custom0":
                        UpdateColorMatrixControls(true);
                        backEnd.setColorCorrectionMatrix(cmb_ColorMatrix.Text);
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="controlStatus"></param>
        private void UpdateColorMatrixControls(bool controlStatus)
        {
            txt_Gain00.Enabled = txt_Gain01.Enabled = txt_Gain02.Enabled = txt_Gain10.Enabled = txt_Gain11.Enabled = txt_Gain12.Enabled = controlStatus;
            txt_Gain20.Enabled = txt_Gain21.Enabled = txt_Gain22.Enabled = controlStatus;
        }

        private void txt_Gain00_TextChanged(object sender, EventArgs e)
        {
            if (backEnd.IsActive())
            {
                backEnd.setGainValue(1,txt_Gain00.Text);
            }
        }
    }
}
