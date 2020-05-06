﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using AITestProject.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Image = System.Windows.Controls.Image;

namespace AITestProject
{
    public partial class MainWindow
    {
        private readonly Stopwatch _detectionTimer = new Stopwatch();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly FilterInfoCollection _filterInfoCollection;

        private readonly CascadeClassifier _faceCascadeClassifier =
            new CascadeClassifier(@"assets\haarcascade_frontalface_default.xml");

        private readonly CascadeClassifier _leftEyeCascadeClassifier =
            new CascadeClassifier(@"assets\haarcascade_lefteye_2splits.xml");

        private readonly CascadeClassifier _rightEyeCascadeClassifier =
            new CascadeClassifier(@"assets\haarcascade_righteye_2splits.xml");

        private VideoCaptureDevice _camera;

        private readonly EnumerableImage _images;

        public MainWindow()
        {
            InitializeComponent();
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Init Images:
            _images = new EnumerableImage(@"assets\LFW");
            NextButton_OnClick(null, null);

            // Init Combobox:
            _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo filter in _filterInfoCollection) DeviceBox.Items.Add(filter.Name);
            DeviceBox.SelectedIndex = 0;

            // Init Webcam:
            _camera = new VideoCaptureDevice(_filterInfoCollection[DeviceBox.SelectedIndex].MonikerString);

            RadioButtonCamera.IsChecked = true;
        }

        // UI:
        private void Window_OnClosed(object sender, EventArgs e)
        {
            _images.Dispose();
            if (_camera == null || !_camera.IsRunning) return;
            _camera.SignalToStop();
            _camera.WaitForStop();
            _camera = null;
            Environment.Exit(Environment.ExitCode);
        }

        private void NextButton_OnClick(object sender, RoutedEventArgs e)
        {
            ClearCanvas();
            Pic.Source = _images.NextImage();

            var grayImage = new Mat(_images.CurrentImagePath()).ToImage<Gray, byte>();
            canvas.Children.Add(
                ImageUtil.EyeTextureImage(Detector.Detect(grayImage, Detector.DetectionObject.LeftEye)));
            canvas.Children.Add(
                ImageUtil.EyeTextureImage(Detector.Detect(grayImage, Detector.DetectionObject.RightEye)));
        }


        private void RadioButtonImage_OnChecked(object sender, RoutedEventArgs e)
        {
            // reset Camera:
            if (_camera != null && _camera.IsRunning)
            {
                _camera.SignalToStop();
                _camera.WaitForStop();
                _camera = null;
            }

            NextButton_OnClick(null, null);
            NextButton.Visibility = Visibility.Visible;
        }

        private void RadioButtonCamera_OnChecked(object sender, RoutedEventArgs e)
        {
            Pic.Source = null;
            ClearCanvas();

            NextButton.Visibility = Visibility.Hidden;
            DeviceBox_OnSelectionChanged(null, null);
        }

        private void ClearCanvas()
        {
            canvas.Children.Clear();
            canvas.Children.Add(Pic);
        }

        //Webcam stuff:
        private void DeviceBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!RadioButtonCamera.IsChecked ?? false) return;
            if (_camera != null && _camera.IsRunning)
            {
                _camera.SignalToStop();
                _camera.WaitForStop();
            }

            _detectionTimer.Start();
            _camera = new VideoCaptureDevice(_filterInfoCollection[DeviceBox.SelectedIndex].MonikerString);
            _camera.NewFrame += Camera_NewFrame;
            _camera.Start();
        }

        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            var bitmap = (Bitmap) eventArgs.Frame.Clone();
            Dispatcher.BeginInvoke((Action) (() => WebcamNextFrame(bitmap)));
        }

        private void WebcamNextFrame(Bitmap bitmap)
        {
            Pic.Source = ImageUtil.GetBitmapImage(bitmap);
            if (_detectionTimer.Elapsed.Seconds < 1) return;
            _detectionTimer.Restart();

            var grayImage = bitmap.ToImage<Gray, byte>();
            ClearCanvas();
            canvas.Children.Add(
                ImageUtil.EyeTextureImage(Detector.Detect(grayImage, Detector.DetectionObject.LeftEye)));
            canvas.Children.Add(
                ImageUtil.EyeTextureImage(Detector.Detect(grayImage, Detector.DetectionObject.RightEye)));
        }
    }
}