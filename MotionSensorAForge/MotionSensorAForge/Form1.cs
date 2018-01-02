using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Configuration;
using System.Media;

namespace MotionSensorAForge
{
    public partial class Form1 : Form
    {
        private BackgroundWorker bgw;
        private string detectedFolderPath;
        private VideoCaptureDevice selectedDevice;
        private FilterInfoCollection deviceCollection;
        private float comparionThreshold = 96;
        private bool referenceImageSet = false;
        private bool cancelBGW = false;
        System.Media.SoundPlayer player = new SoundPlayer("doorbell-2.wav");

        private int Count
        {
            set
            {
                lblCount.Text = value.ToString();
                lblCount.Refresh();
            }
            get
            {
                return int.Parse(lblCount.Text);
            }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                deviceCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo device in deviceCollection)
                {
                    cmbCameraList.Items.Add(device);
                }
                cmbCameraList.DisplayMember = "Name";
                detectedFolderPath = ConfigurationManager.AppSettings["ImageLocation"];
                BtnStartStop.Enabled = false;
                bgw = new BackgroundWorker();
                bgw.DoWork += Bgw_DoWork;
                bgw.WorkerReportsProgress = true;
                bgw.WorkerSupportsCancellation = true;
                bgw.ProgressChanged += Bgw_ProgressChanged;
                bgw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while detecting Video Devices. Please restart the software. \n" + ex.Message);
            }
        }

        private void cmbCameraList_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedDevice = new VideoCaptureDevice((cmbCameraList.SelectedItem as FilterInfo).MonikerString);
            BtnStartStop.Enabled = true;
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (selectedDevice == null)
                return;
            if (selectedDevice.IsRunning)
            {   //stop
                selectedDevice.NewFrame -= SelectedDevice_NewFrame;
                selectedDevice.Stop();
                pictureBoxReferenceImage.Image = null;
                referenceImageSet = false;
                pictureBox1.Image = null;
            }
            else
            {   //start
                selectedDevice.NewFrame += SelectedDevice_NewFrame;
                comparionThreshold = trackBar1.Value;
                selectedDevice.Start();
                Count = 0;
            }
        }
        private void SelectedDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap image = (Bitmap)eventArgs.Frame.Clone();
            pictureBox1.Image = image;
            if (!referenceImageSet)
            {
                pictureBoxReferenceImage.Image = image;
                referenceImageSet = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (selectedDevice != null && selectedDevice.IsRunning)
            {
                selectedDevice.NewFrame -= SelectedDevice_NewFrame;
                selectedDevice.Stop();
                bgw.CancelAsync();
            }
        }

        private void BtnSetReference_Click(object sender, EventArgs e)
        {
            referenceImageSet = false;
            Count = 0;
        }

        private void btnOpenDetectedImages_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(detectedFolderPath))
                System.Diagnostics.Process.Start(detectedFolderPath);
            else
                MessageBox.Show("No Images Found");
        }

        private float compareImages(Bitmap image1, Bitmap image2)
        {
            var tm = new AForge.Imaging.ExhaustiveTemplateMatching();
            var result = tm.ProcessImage(image1, image2);
            if (result.Length <= 0)
                return 0;

            return result[0].Similarity;
        }
        private void Bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (selectedDevice == null || !selectedDevice.IsRunning)
                return;
            if (pictureBox1.Image == null || pictureBoxReferenceImage.Image == null)
                return;
            Bitmap img1 = (Bitmap)pictureBox1.Image.Clone();
            Bitmap img2 = (Bitmap)pictureBoxReferenceImage.Image.Clone();
            bool flag = true;
            if (img1.Width == img2.Width && img1.Height == img2.Height)
            {
                float c = compareImages(img1, img2);
                if (c < comparionThreshold/100.00)
                    flag = false;
            }
            else { flag = false; }
            if (flag == false)
            {
                //selectedDevice.Stop();
                //selectedDevice.NewFrame -= SelectedDevice_NewFrame;
                //MessageBox.Show("Sorry, Images are not same");
                Count = Count + 1;
                playDoorBell();
                System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(new Action(() => saveImage(img1)));
                t.Start();
            }
        }
        private void saveImage(Bitmap image)
        {
            if(!System.IO.Directory.Exists(detectedFolderPath))
                System.IO.Directory.CreateDirectory(detectedFolderPath);
            if (System.IO.Directory.Exists(detectedFolderPath))
                image.Save(detectedFolderPath + "Detected_" + DateTime.Now.ToString("ddMMMyyyyhhmmss")+".jpg");
        }
        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (1 == 1 && bgw.CancellationPending == false)
            {
                System.Threading.Thread.Sleep(1000);
                bgw.ReportProgress(1);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReferenceImage ri = new ReferenceImage(pictureBoxReferenceImage.Image);
            ri.ShowDialog();
        }

        private void playDoorBell()
        {
            System.Media.SoundPlayer player = new SoundPlayer("doorbell-2.wav");
            player.Play();
        }
    }
}
