/*!
 * \file    BackEnd.cs
 * \author  IDS Imaging Development Systems GmbH
 * \date    2022-06-01
 * \since   1.1.6
 *
 * \version 1.1.0
 *
 * Copyright (C) 2020 - 2022, IDS Imaging Development Systems GmbH.
 *
 * The information in this document is subject to change without notice
 * and should not be construed as a commitment by IDS Imaging Development Systems GmbH.
 * IDS Imaging Development Systems GmbH does not assume any responsibility for any errors
 * that may appear in this document.
 *
 * This document, or source code, is provided solely as an example of how to utilize
 * IDS Imaging Development Systems GmbH software libraries in a sample application.
 * IDS Imaging Development Systems GmbH does not assume any responsibility
 * for the use or reliability of any portion of this document.
 *
 * General permission to copy or modify is hereby granted.
 */

using System;
using System.Linq;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace SampleApplication
{
    class BackEnd
    {
        // Event which is raised if a new image was received
        public delegate void ImageReceivedEventHandler(object sender, Bitmap image);
        public event ImageReceivedEventHandler ImageReceived;

        // Event which is raised if the counters has changed
        public delegate void CounterChangedEventHandler(object sender, uint frameCounter, uint errorCounter);
        public event CounterChangedEventHandler CounterChanged;

        // Event which is raised if an Error or Exception has occurred
        public delegate void MessageBoxTriggerEventHandler(object sender, String messageTitle, String messageText);
        public event MessageBoxTriggerEventHandler MessageBoxTrigger;


        // Event which is raised if the counters has changed
        public delegate void SaveImageHandler(object sender, bool savedImage);
        public event SaveImageHandler SaveImage;

        private AcquisitionWorker acquisitionWorker;
        private Thread acquisitionThread;

        private peak.core.Device device;
        private peak.core.DataStream dataStream;
        private peak.core.NodeMap nodeMapRemoteDevice;

        private bool isActive;

        public string imageLocation = string.Empty;

        public BackEnd()
        {
            Debug.WriteLine("--- [BackEnd] Init");

            isActive = true;

            try
            {
                // Create acquisition worker thread that waits for new images from the camera
                acquisitionWorker = new AcquisitionWorker();
                acquisitionThread = new Thread(new ThreadStart(acquisitionWorker.Start));

                acquisitionWorker.ImageReceived += acquisitionWorker_ImageReceived;
                acquisitionWorker.CounterChanged += acquisitionWorker_CounterChanged;
                acquisitionWorker.MessageBoxTrigger += acquisitionWorker_MessageBoxTrigger;

                
            }
            catch (Exception e)
            {
                Debug.WriteLine("--- [BackEnd] Exception: " + e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool start()
        {
            Debug.WriteLine("--- [BackEnd] Start");

            // Initialize peak library
            peak.Library.Initialize();

            if (!OpenDevice())
            {
                return false;
            }

            // Start thread execution
            //if ( acquisitionThread.ThreadState == System.Threading.ThreadState.Unstarted )
            //        acquisitionThread.Start();

            switch(acquisitionThread.ThreadState)
            {
                case System.Threading.ThreadState.Unstarted:
                    acquisitionThread.Start();
                    break;
                case System.Threading.ThreadState.Stopped:

                    if (!acquisitionThread.IsAlive)
                    {
                        acquisitionThread = new Thread(new ThreadStart(acquisitionWorker.Start));
                    }
                   
                    break;
            }


            return true;
        }



        public void Stop()
        {
            Debug.WriteLine("--- [BackEnd] Stop");
            isActive = false;
            acquisitionWorker.Stop();

            if (acquisitionThread.IsAlive)
            {
                acquisitionThread.Join();
            }

            CloseDevice();

            // Close peak library
            peak.Library.Close();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool OpenDevice()
        {
            Debug.WriteLine("--- [BackEnd] Open device");
            try
            {
                // Create instance of the device manager
                var deviceManager = peak.DeviceManager.Instance();

                // Update the device manager
                deviceManager.Update();

                // Return if no device was found
                if (!deviceManager.Devices().Any())
                {
                    Debug.WriteLine("--- [BackEnd] Error: No device found");
                    MessageBoxTrigger(this, "Error", "No device found");
                    return false;
                }

                // Open the first openable device in the device manager's device list
                var deviceCount = deviceManager.Devices().Count();

                for (var i = 0; i < deviceCount; ++i)
                {
                    if (deviceManager.Devices()[i].IsOpenable())
                    {
                        device = deviceManager.Devices()[i].OpenDevice(peak.core.DeviceAccessType.Control);

                        // Stop after the first opened device
                        break;
                    }
                    else if (i == (deviceCount - 1))
                    {
                        Debug.WriteLine("--- [BackEnd] Error: Device could not be openend");
                        MessageBoxTrigger(this, "Error", "Device could not be openend");
                        return false;
                    }
                }

                if (device != null)
                {
                    // Check if any datastreams are available
                    var dataStreams = device.DataStreams();

                    if (!dataStreams.Any())
                    {
                        Debug.WriteLine("--- [BackEnd] Error: Device has no DataStream");
                        MessageBoxTrigger(this, "Error", "Device has no DataStream");
                        return false;
                    }

                    try
                    {
                        // Open standard data stream
                        dataStream = dataStreams[0].OpenDataStream();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("--- [BackEnd] Error: Failed to open DataStream");
                        MessageBoxTrigger(this, "Error", "Failed to open DataStream\n" + e.Message);
                        return false;
                    }

                    // Get nodemap of remote device for all accesses to the genicam nodemap tree
                    nodeMapRemoteDevice = device.RemoteDevice().NodeMaps()[0];

                    // To prepare for untriggered continuous image acquisition, load the default user set if available
                    // and wait until execution is finished
                    try
                    {
                        nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("UserSetSelector").SetCurrentEntry("Default");
                        nodeMapRemoteDevice.FindNode<peak.core.nodes.CommandNode>("UserSetLoad").Execute();
                        nodeMapRemoteDevice.FindNode<peak.core.nodes.CommandNode>("UserSetLoad").WaitUntilDone();
                    }
                    catch
                    {
                        // UserSet is not available
                    }

                    // Get the payload size for correct buffer allocation
                    UInt32 payloadSize = Convert.ToUInt32(nodeMapRemoteDevice.FindNode<peak.core.nodes.IntegerNode>("PayloadSize").Value());

                    // Get the minimum number of buffers that must be announced
                    var bufferCountMax = dataStream.NumBuffersAnnouncedMinRequired();

                    // Allocate and announce image buffers and queue them
                    for (var bufferCount = 0; bufferCount < bufferCountMax; ++bufferCount)
                    {
                        var buffer = dataStream.AllocAndAnnounceBuffer(payloadSize, IntPtr.Zero);
                        dataStream.QueueBuffer(buffer);
                    }

                    // Configure worker
                    acquisitionWorker.SetDataStream(dataStream);
                    acquisitionWorker.SetNodemapRemoteDevice(nodeMapRemoteDevice);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("--- [BackEnd] Exception: " + e.Message);
                MessageBoxTrigger(this, "Exception", e.Message);
                return false;
            }

            return true;
        }

        public void CloseDevice()
        {
            Debug.WriteLine("--- [BackEnd] Close device");
            // If device was opened, try to stop acquisition
            if (device != null)
            {
                try
                {
                    var remoteNodeMap = device.RemoteDevice().NodeMaps()[0];
                    remoteNodeMap.FindNode<peak.core.nodes.CommandNode>("AcquisitionStop").Execute();
                    remoteNodeMap.FindNode<peak.core.nodes.CommandNode>("AcquisitionStop").WaitUntilDone();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("--- [BackEnd] Exception: " + e.Message);
                    MessageBoxTrigger(this, "Exception", e.Message);
                }
            }

            // If data stream was opened, try to stop it and revoke its image buffers
            if (dataStream != null)
            {
                try
                {
                    dataStream.KillWait();
                    dataStream.StopAcquisition(peak.core.AcquisitionStopMode.Default);
                    dataStream.Flush(peak.core.DataStreamFlushMode.DiscardAll);

                    foreach (var buffer in dataStream.AnnouncedBuffers())
                    {
                        dataStream.RevokeBuffer(buffer);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("--- [BackEnd] Exception: " + e.Message);
                    MessageBoxTrigger(this, "Exception", e.Message);
                }
            }

            try
            {
                // Unlock parameters after acquisition stop
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.IntegerNode>("TLParamsLocked").SetValue(0);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("--- [BackEnd] Exception: " + e.Message);
                MessageBoxTrigger(this, "Exception", e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameRate"></param>
        public void setFrameRate(decimal frameRate)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("AcquisitionFrameRate").SetValue((double)frameRate);
                }
            }
            catch(Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameRate"></param>
        public void setExposureTime(decimal exposureTime)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ExposureTime").SetValue((double)exposureTime);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameRate"></param>
        public void setAnalogGain(decimal analogGain)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    // Set GainSelector to "AnalogAll"
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainSelector").SetCurrentEntry("AnalogAll");
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("Gain").SetValue((double)analogGain);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        internal void setReverseX(bool reverxChecked)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.BooleanNode>("ReverseX").SetValue(reverxChecked);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        internal void setReverseY(bool reveryChecked)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.BooleanNode>("ReverseY").SetValue(reveryChecked);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        public void setDigitalgGain(decimal digitalGain)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    // Set GainSelector to "AnalogAll"
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainSelector").SetCurrentEntry("DigitalAll");
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("Gain").SetValue((double)digitalGain);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this,"Exception", ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void setAutoExpo(string value)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ExposureAuto").SetCurrentEntry(value);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

  


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void setAutoGain(string value)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainAuto").SetCurrentEntry(value);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        public void setPercentile(decimal percentile)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("BrightnessAutoPercentile").SetValue((double)percentile);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }



        public void setTarget(int target)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.IntegerNode>("BrightnessAutoTarget").SetValue(target);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }



        public void setTolrence(int tolorence)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.IntegerNode>("BrightnessAutoTargetTolerance").SetValue(tolorence);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        public void setAutoWhiteBalance(string value)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("BalanceWhiteAuto").SetCurrentEntry(value);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }



        public void setDigitalgRGain(decimal rGain)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainSelector").SetCurrentEntry("DigitalRed");
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("Gain").SetValue((double)rGain);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }

        public void setDigitalgGGain(decimal gGain)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainSelector").SetCurrentEntry("DigitalGreen");
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("Gain").SetValue((double)gGain);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        public void setDigitalgBGain(decimal bGain)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainSelector").SetCurrentEntry("DigitalBlue");
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("Gain").SetValue((double)bGain);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }



        public void setColorCorrectionMode(string colorCorrectionMode)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMode").SetCurrentEntry(colorCorrectionMode);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        public void setColorCorrectionMatrix(string colorCorrectionMateix)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrix").SetCurrentEntry(colorCorrectionMateix);
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }


        internal void setGainValue(int matrixPos, string gainValue)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    switch(matrixPos)
                    {
                        case 1:
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain00");
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").SetValue(Convert.ToDouble(gainValue));
                            break;
                        case 2:
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain01");
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").SetValue(Convert.ToDouble(gainValue));
                            break;
                        case 3:
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain02");
                            nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").SetValue(Convert.ToDouble(gainValue));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Exception", ex.Message);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceConfig"></param>
        internal void GetConfigration(ref Global.DeviceConfig deviceConfig)
        {
            try
            {
                if (nodeMapRemoteDevice != null)
                {
                    // Frame Rate
                    deviceConfig.frameRate =  Convert.ToDecimal(nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("AcquisitionFrameRate").Value());

                    // Exposure Time
                    deviceConfig.exposureTime = Convert.ToDecimal(nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ExposureTime").Value());


                    // Auto Exposure
                    deviceConfig.exposureAuto = nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ExposureAuto").CurrentEntry().SymbolicValue();
                    deviceConfig.gainAuto = nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("GainAuto").CurrentEntry().SymbolicValue();

                    // Percentile 
                    deviceConfig.percentile = Convert.ToDecimal(nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("BrightnessAutoPercentile").Value());

                    // Gain 00
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain00");
                    deviceConfig.gainMatrix.gain00 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();

                    // Gain01
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain01");
                    deviceConfig.gainMatrix.gain01 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();


                    // Gain02
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain02");
                    deviceConfig.gainMatrix.gain02 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();


                    // Gain10
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain10");
                    deviceConfig.gainMatrix.gain10 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();

                    // Gain11
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain11");
                    deviceConfig.gainMatrix.gain11 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();

                    // Gain12
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain12");
                    deviceConfig.gainMatrix.gain12 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();


                    // Gain20
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain20");
                    deviceConfig.gainMatrix.gain20 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();

                    // Gain21
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain21");
                    deviceConfig.gainMatrix.gain21 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();

                    // Gain22
                    nodeMapRemoteDevice.FindNode<peak.core.nodes.EnumerationNode>("ColorCorrectionMatrixValueSelector").SetCurrentEntry("Gain22");
                    deviceConfig.gainMatrix.gain22 = nodeMapRemoteDevice.FindNode<peak.core.nodes.FloatNode>("ColorCorrectionMatrixValue").Value();


                }
            }
            catch (Exception ex)
            {
                MessageBoxTrigger(this, "Error Getting Device Configuraiton with Exception ", ex.Message);
            }
        }



        private void acquisitionWorker_ImageReceived(object sender, System.Drawing.Bitmap image)
        {
            ImageReceived(sender, image);
        }

        private void acquisitionWorker_CounterChanged(object sender, uint frameCounter, uint errorCounter)
        {
            CounterChanged(sender, frameCounter, errorCounter);
        }

        private void acquisitionWorker_MessageBoxTrigger(object sender, String messageTitle, String messageText)
        {
            MessageBoxTrigger(sender, messageTitle, messageText);
        }

        public bool IsActive()
        {
            return isActive;
        }
    }
}
