using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
using Envision.Common;

namespace Envision_CamLib
{
    public partial class frm_uEyeInitNew : KryptonForm
    {
        public bool m_bAeOp = false;
        public int m_n_dev_count = 0;


        public string _applicationPath = string.Empty;

        public string _imageLocation = string.Empty;
        private Boolean b_isIDSPeak = false;
        public bool _isWeldMet = false;


        public delegate void LoadImageEvent();
        public LoadImageEvent _loadRefreshimages;

        public string _jobID = string.Empty;



        uEye.Camera m_Camera;
        private uint m_u32ImageWidth = 0;
        private uint m_u32ImageHeight = 0;

        // overlay moving
        private Int32 m_s32MovePosX = 1;
        private Int32 m_s32MovePosY = 1;

        // overlay position
        private UInt32 m_u32OverlayPositionX = 0;
        private UInt32 m_u32OverlayPositionY = 0;

        private System.Drawing.Color m_OverlayColor = System.Drawing.Color.Black;

        // overlay moving update timer
        Timer m_OverlayMoveTimer = new Timer();

        // DirectRendererOverlay
        uEye.DirectRendererOverlay m_Overlay = null;
        public frm_uEyeInitNew()
        {
            InitializeComponent();
        }

        private void chkb_Properties_CheckedChanged(object sender, EventArgs e)
        {
            splt_Container.Panel2Collapsed = !chkb_CameraSettings.Checked;
        }

        private void frm_uEyeInit_Shown(object sender, EventArgs e)
        {
            bool bDirect3D = false;
            bool bOpenGL = false;


            uEye.Defines.Status statusRet;
            m_Camera = new uEye.Camera();
            m_Overlay = new uEye.DirectRendererOverlay(m_Camera);

            cB_Semi_transparent.Enabled = false;

            // open first available camera
            statusRet = m_Camera.Init(0, DisplayWindow.Handle.ToInt32());
            if (statusRet == uEye.Defines.Status.SUCCESS)
            {
                m_OverlayMoveTimer.Interval = 10;
                m_OverlayMoveTimer.Tick += new EventHandler(OnOverlayMove);

                statusRet = m_Camera.PixelFormat.Set(uEye.Defines.ColorMode.BGR8Packed);

                uEye.Defines.DisplayMode supportedMode;
                statusRet = m_Camera.DirectRenderer.GetSupported(out supportedMode);

                if ((supportedMode & uEye.Defines.DisplayMode.Direct3D) == uEye.Defines.DisplayMode.Direct3D)
                {
                    rB_Direct3D.Enabled = true;
                    bDirect3D = true;
                }
                else
                {
                    rB_Direct3D.Enabled = false;
                    bDirect3D = false;
                }

                if ((supportedMode & uEye.Defines.DisplayMode.OpenGL) == uEye.Defines.DisplayMode.OpenGL)
                {
                    rB_OpenGL.Enabled = true;
                    bOpenGL = true;

                    if (rB_Direct3D.Enabled != true)
                    {
                        rB_OpenGL.Checked = true;
                    }
                }
                else
                {
                    rB_OpenGL.Enabled = false;
                    bOpenGL = false;
                }

                if (((supportedMode & uEye.Defines.DisplayMode.Direct3D) == uEye.Defines.DisplayMode.Direct3D) ||
                    ((supportedMode & uEye.Defines.DisplayMode.OpenGL) == uEye.Defines.DisplayMode.OpenGL))
                {

                    if (bOpenGL == true)
                    {
                        // set display mode
                        statusRet = m_Camera.Display.Mode.Set(uEye.Defines.DisplayMode.OpenGL);
                    }

                    if (bDirect3D == true)
                    {
                        // set display mode
                        statusRet = m_Camera.Display.Mode.Set(uEye.Defines.DisplayMode.Direct3D);
                    }

                    // start live
                    BUTTON_START_Click(null, EventArgs.Empty);

                    // update information
                    UpdateOverlayInformation();
                    UpdateImageInformation();

                }
                else
                {
                    MessageBox.Show("Direct3D and OpenGL are not supported");
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Could not open camera...");
                //Close();
            }

            
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateOverlayInformation()
        {
            uEye.Types.Size<UInt32> overlaySize;
            uEye.Defines.Status statusRet;

            statusRet = m_Overlay.GetSize(out overlaySize);

            tB_Overlay_Width.Text = overlaySize.Width.ToString();
            tB_Overlay_Height.Text = overlaySize.Height.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateImageInformation()
        {
            /* open the camera */
            System.Drawing.Rectangle imageRect;
            uEye.Defines.Status statusRet;

            statusRet = m_Camera.Size.AOI.Get(out imageRect);

            m_u32ImageWidth = (uint)imageRect.Width;
            m_u32ImageHeight = (uint)imageRect.Height;

            /* Image Info*/
            tB_Image_Width.Text = imageRect.Width.ToString();
            tB_Image_Height.Text = imageRect.Height.ToString();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnOverlayMove(object source, EventArgs e)
        {
            m_Overlay.SetPosition(m_u32OverlayPositionX, m_u32OverlayPositionY);
            m_Overlay.Show();

            // force display update
            if (BUTTON_START.Enabled)
            {
                m_Camera.DirectRenderer.Update();
            }

            if (m_u32OverlayPositionX > this.m_u32ImageWidth)
            {
                m_s32MovePosX = -1;
            }

            if (m_u32OverlayPositionY > this.m_u32ImageHeight)
            {
                m_s32MovePosY = -1;
            }

            if (m_u32OverlayPositionX == 0)
            {
                m_s32MovePosX = 1;
            }

            if (m_u32OverlayPositionY == 0)
            {
                m_s32MovePosY = 1;
            }

            m_u32OverlayPositionX = (uint)((int)m_u32OverlayPositionX + m_s32MovePosX);
            m_u32OverlayPositionY = (uint)((int)m_u32OverlayPositionY + m_s32MovePosY);
        }

        private void BUTTON_START_Click(object sender, EventArgs e)
        {
            /* Start the camera acquisition */
            uEye.Defines.Status statusRet;
            statusRet = m_Camera.Acquisition.Capture();

            /* active keys Strart Live and Stop Live */
            BUTTON_START.Enabled = false;
            BUTTON_STOP.Enabled = true;
            BUTTON_SAVE.Enabled = true;
            chkb_CameraSettings.Enabled = true;
        }

        private void BUTTON_STOP_Click(object sender, EventArgs e)
        {
            /* Stop the camera acquisition */
            uEye.Defines.Status statusRet;
            statusRet = m_Camera.Acquisition.Stop();

            /* active keys Strart Live and Stop Live */
            BUTTON_START.Enabled = true;
            BUTTON_STOP.Enabled = false;
            BUTTON_SAVE.Enabled = false;
            chkb_CameraSettings.Enabled = false;
        }

        private void BUTTON_SAVE_Click(object sender, EventArgs e)
        {


            string file_path = string.Empty;

            Random rn = new Random();
            string rnd = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            if (_isWeldMet)
            {
                file_path = _imageLocation + "\\Images\\" + _jobID + "_" + "x" + "_" + rnd + ".bmp";
            }
            else
            {
                file_path = _imageLocation + "\\" + "IM_IMG_X" + "_" + rnd + ".bmp";
            }

            m_Camera.Image.Save(file_path, uEye.Defines.ImageFormat.Bmp);

            MessageBox.Show(file_path + " Image Saved Successfully !!");

            if (_loadRefreshimages != null)
            {
                _loadRefreshimages();
            }
            
            if (_loadRefreshimages != null)
            {
                _loadRefreshimages();
            }
        }

        private void frm_uEyeInit_Load(object sender, EventArgs e)
        {
            splt_Container.Panel2Collapsed = !chkb_CameraSettings.Checked;
        }

        private void frm_uEyeInit_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_Camera.Exit();
        }

        private void cbhkb_AutoGain_CheckedChanged(object sender, EventArgs e)
        {
            m_Camera.AutoFeatures.Sensor.GainShutter.SetEnable(cbhkb_AutoGain.Checked);
            EnDisGainControls(cbhkb_AutoGain.Checked);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checked"></param>
        private void EnDisGainControls(bool @checked)
        {
            //GainEdit.Enabled = @checked;
            //GainApply.Enabled = @checked;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AWBEnable_CheckedChanged(object sender, EventArgs e)
        {
            m_Camera.AutoFeatures.Software.WhiteBalance.SetEnable(AWBEnable.Checked);
            cbhkb_AutoGain.Enabled = AWBEnable.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmb_AWMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
                ADOBE_RGB_D65
                CIE_RGB_E
                ECI_RGB_D50
                SRGB_D50
                SRGB_D65
             */

            switch(cmb_AWMode.SelectedItem.ToString())
            {
                case "ADOBE_RGB_D65":
                    m_Camera.AutoFeatures.Software.WhiteBalance.SetColorModel(uEye.Defines.ColorTemperatureRgbMode.ADOBE_RGB_D65);
                    break;
                case "CIE_RGB_E":
                    m_Camera.AutoFeatures.Software.WhiteBalance.SetColorModel(uEye.Defines.ColorTemperatureRgbMode.CIE_RGB_E);
                    break;
                case "ECI_RGB_D50":
                    m_Camera.AutoFeatures.Software.WhiteBalance.SetColorModel(uEye.Defines.ColorTemperatureRgbMode.ECI_RGB_D50);
                    break;
                case "SRGB_D50":
                    m_Camera.AutoFeatures.Software.WhiteBalance.SetColorModel(uEye.Defines.ColorTemperatureRgbMode.SRGB_D50);
                    break;
                case "SRGB_D65":
                    m_Camera.AutoFeatures.Software.WhiteBalance.SetColorModel(uEye.Defines.ColorTemperatureRgbMode.SRGB_D65);
                    break;
            }
        }

        private void COMBO_AE_MODE_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
                None
                CenterWeighted
                CenterSpot
                Portrait
                Landscape
            */
            switch (COMBO_AE_MODE.SelectedItem.ToString())
            {
                case "None":
                    m_Camera.AutoFeatures.Sensor.Gain.SetPhotom(uEye.Defines.Whitebalance.GainPhotomMode.None);
                    break;
                case "CenterWeighted":
                    m_Camera.AutoFeatures.Sensor.Gain.SetPhotom(uEye.Defines.Whitebalance.GainPhotomMode.None);
                    break;
                case "CenterSpot":
                    m_Camera.AutoFeatures.Sensor.Gain.SetPhotom(uEye.Defines.Whitebalance.GainPhotomMode.CenterSpot);
                    break;
                case "Portrait":
                    m_Camera.AutoFeatures.Sensor.Gain.SetPhotom(uEye.Defines.Whitebalance.GainPhotomMode.Portrait);
                    break;
                case "Landscape":
                    m_Camera.AutoFeatures.Sensor.Gain.SetPhotom(uEye.Defines.Whitebalance.GainPhotomMode.Landscape);
                    break;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_Contrast_CheckedChanged(object sender, EventArgs e)
        {
            tbar_Contrast.Enabled = chkb_Contrast.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_Gamma_CheckedChanged(object sender, EventArgs e)
        {
            int GamValue = 0;
            tbar_Gamma.Enabled = chkb_Gamma.Checked;
            m_Camera.Gamma.Software.Get(out GamValue);
            tbar_Gamma.Value = GamValue;
        }

        private void chkb_BlackLevel_CheckedChanged(object sender, EventArgs e)
        {
            tbar_BlackLevel.Enabled = chkb_BlackLevel.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_Sharpness_CheckedChanged(object sender, EventArgs e)
        {
            tbar_Sharpness.Enabled = chkb_Sharpness.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_2DNoise_CheckedChanged(object sender, EventArgs e)
        {
            tbar_2DNoise.Enabled = chkb_2DNoise.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_3DNoise_CheckedChanged(object sender, EventArgs e)
        {
            tbar_3DNoise.Enabled = chkb_3DNoise.Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_VFlip_CheckedChanged(object sender, EventArgs e)
        {
            m_Camera.RopEffect.Set(uEye.Defines.RopEffectMode.UpDown, chkb_VFlip.Checked);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkb_HFlip_CheckedChanged(object sender, EventArgs e)
        {
            m_Camera.RopEffect.Set(uEye.Defines.RopEffectMode.LeftRight, chkb_HFlip.Checked);
        }

        private void chkb_Mono_CheckedChanged(object sender, EventArgs e)
        {
            uEye.Defines.DisplayMode displayMode;
            m_Camera.Display.Mode.Get(out displayMode);

            if (chkb_Mono.Checked == true)
            {
                displayMode |= uEye.Defines.DisplayMode.Mono;
            }
            else
            {
                displayMode &= ~uEye.Defines.DisplayMode.Mono;
            }

            m_Camera.Display.Mode.Set(displayMode);
        }

        private void tbar_Gamma_Scroll(object sender, EventArgs e)
        {
            m_Camera.Gamma.Software.Set(tbar_Gamma.Value);
        }

        private void tbar_BlackLevel_Scroll(object sender, EventArgs e)
        {
            m_Camera.BlackLevel.Offset.Set(tbar_BlackLevel.Value);
        }

        private void tbar_Sharpness_Scroll(object sender, EventArgs e)
        {
            m_Camera.Saturation.Set(tbar_Sharpness.Value);
        }

        private void tbar_Contrast_Scroll(object sender, EventArgs e)
        {

        }
    }
}
