using Emgu.CV;
using Emgu.CV.Structure;
using FlyCapture2Managed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageProcessing_BSC_WPF.Properties.Settings;

namespace ImageProcessing_BSC_WPF.Modules
{
    class PTCam
    {
        public static PTCam mPTCam = new PTCam();

        public enum CamName
        {
            dCam,
        }
        private CamName mCamName;

        //====================
        public const int NUM_CAMS = 1;
        public int NumCameras;

        public uint[] CamSerialList;

        public bool CamConnectionError;

        public ManagedGigECamera[] mCameras = new ManagedGigECamera[NUM_CAMS];
        private ManagedImage m_rawImage = new ManagedImage();
        private ManagedImage m_processedImage = new ManagedImage();

        public PTCam()
        {
            CamSerialList = new uint[NUM_CAMS];
            CamSerialList[0] = Convert.ToUInt32(Default.ptCamSerial);
        }

        public bool CamConnection(uint[] serialList)
        {
            NumCameras = serialList.Length;
            ManagedBusManager busMgr = new ManagedBusManager();
            mCameras = new ManagedGigECamera[NumCameras];

            for (uint i = 0; i < NumCameras; i++)
            {
                if (serialList[i] == 0) continue;

                mCameras[i] = new ManagedGigECamera();

                try
                {
                    ManagedPGRGuid guid = busMgr.GetCameraFromSerialNumber(serialList[i]);

                    // Connect to a camera
                    mCameras[i].Connect(guid);

                    // Turn trigger mode off
                    TriggerMode trigMode = new TriggerMode();
                    trigMode.onOff = false;
                    mCameras[i].SetTriggerMode(trigMode);

                    // Turn Timestamp on
                    EmbeddedImageInfo imageInfo = new EmbeddedImageInfo();
                    imageInfo.timestamp.onOff = true;
                    mCameras[i].SetEmbeddedImageInfo(imageInfo);

                    //IsConnected[i] = true;
                }
                catch (Exception ex)
                {

                    //IsConnected[i] = false;
                    return false;
                }
            }
            return true;
        }

        public void SetModeAndStartCapture(ManagedGigECamera camera, Mode mode)
        {
            try
            {
                camera.SetGigEImagingMode(mode);
                // Start streaming on camera
                camera.StartCapture();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting camera : {0}", ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
                return;
            }
        }

        /// <summary>
        /// Main version of capture
        /// </summary>
        /// <param name="Angle"></param>
        /// <param name="cropOn"></param>
        /// <returns></returns>
        public Image<Bgr, byte> capture(ManagedGigECamera camera, int Angle) //, bool cropOn)
        {
            Image<Bgr, byte> bmp = null;
            if (camera == null) return bmp;
            try
            {
                camera.RetrieveBuffer(m_rawImage);
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }
            lock (this)
            {
                m_rawImage.Convert(FlyCapture2Managed.PixelFormat.PixelFormatBgr, m_processedImage);
                bmp = new Image<Bgr, byte>(m_processedImage.bitmap);
            }

            //if (Angle != 0) bmp = bmp.Convert<Bgr, Byte>().Rotate(Angle, new Bgr(0, 0, 0), cropOn);
            if (Angle != 0) bmp = bmp.Convert<Bgr, Byte>().Rotate(Angle, new Bgr(0, 0, 0), false);
            return bmp;
        }

        public void DisconnectCam()
        {
            for (uint i = 0; i < NumCameras; i++)
            {
                if (CamSerialList[i] == 0) continue;

                try
                {
                    mCameras[i].StopCapture();
                    mCameras[i].Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error cleaning up camera : {0}", ex.Message);
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadLine();
                    return;
                }
            }
        }
    }
}
