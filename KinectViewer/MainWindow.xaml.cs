//Achille Pisani's KinectViewer Project


using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectViewer
{
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;

        private WriteableBitmap depthBitmap;
        private short[] depthPixels;
        private byte[] depthPixelsByte;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.KinectSensors[0];

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.SkeletonStream.Enable();

            colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
            colorBitmap = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
            colorImage.Source = colorBitmap;

            depthPixels = new short[sensor.DepthStream.FramePixelDataLength];
            depthPixelsByte = new byte[depthPixels.Length];
            depthBitmap = new WriteableBitmap(320, 240, 96.0, 96.0, PixelFormats.Gray8, null);
            depthImage.Source = depthBitmap;

            sensor.ColorFrameReady += Sensor_ColorFrameReady;
            sensor.DepthFrameReady += Sensor_DepthFrameReady;
            sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

            sensor.Start();
        }

        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null) return;
                frame.CopyPixelDataTo(colorPixels);
                colorBitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height),
                    colorPixels, frame.Width * 4, 0);
            }
        }

        private void Sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null) return;
                frame.CopyPixelDataTo(depthPixels);

                for (int i = 0; i < depthPixels.Length; ++i)
                {
                    int depth = depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    byte intensity = (byte)(depth >= 800 && depth <= 4000 ? depth / 16 : 0);
                    depthPixelsByte[i] = intensity;
                }

                depthBitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height),
                    depthPixelsByte, frame.Width, 0);
            }
        }

        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null) return;
                Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                skeletonCanvas.Children.Clear();

                foreach (var skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        DrawBone(skel, JointType.Head, JointType.ShoulderCenter);
                        DrawBone(skel, JointType.ShoulderCenter, JointType.Spine);
                        DrawBone(skel, JointType.Spine, JointType.HipCenter);
                        DrawBone(skel, JointType.ShoulderCenter, JointType.ShoulderLeft);
                        DrawBone(skel, JointType.ShoulderCenter, JointType.ShoulderRight);
                        // puoi aggiungere altri segmenti
                    }
                }
            }
        }

        private void DrawBone(Skeleton skeleton, JointType j1, JointType j2)
        {
            Joint joint1 = skeleton.Joints[j1];
            Joint joint2 = skeleton.Joints[j2];

            if (joint1.TrackingState != JointTrackingState.NotTracked &&
                joint2.TrackingState != JointTrackingState.NotTracked)
            {
                var p1 = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint1.Position, ColorImageFormat.RgbResolution640x480Fps30);
                var p2 = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint2.Position, ColorImageFormat.RgbResolution640x480Fps30);

                var line = new System.Windows.Shapes.Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                skeletonCanvas.Children.Add(line);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sensor != null && sensor.IsRunning)
            {
                sensor.Stop();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

