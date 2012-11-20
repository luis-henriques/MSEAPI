﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MSEKinect;
using MSELocator;

namespace RoomVisualizer
{
    /// <summary>
    /// Interaction logic for DeviceControl.xaml
    /// </summary>
    public partial class DeviceControl : UserControl
    {
        private enum DisplayState
        {
            UnpairedAndOnStackPanel,
            PairedAndOnCanvas,
        }


        // DeviceControl can be displayed on the room visualizer canvas, or the stack panel of unpaired devices.
        private DisplayState myDisplayState;
        private DisplayState MyDisplayState
        {
            get
            {
                return myDisplayState;
            }
            set
            {
                if (value == DisplayState.PairedAndOnCanvas && myDisplayState == DisplayState.UnpairedAndOnStackPanel)
                {
                    //Handle transition to display on Canvas
                    MainWindow.SharedDeviceStackPanel.Children.Remove(this);

                    formatForCanvas();

                    MainWindow.SharedCanvas.Children.Add(this);




                }
                else if (value == DisplayState.UnpairedAndOnStackPanel && myDisplayState == DisplayState.PairedAndOnCanvas)
                {
                    //Handle transition to display on StackPanel
                    MainWindow.SharedCanvas.Children.Remove(this);

                    formatForStackPanel();

                    MainWindow.SharedDeviceStackPanel.Children.Add(this);
                }

                myDisplayState = value;
            }
        }

        public void formatForCanvas()
        {
            double deviceSize = 0.5 * MainWindow.SharedCanvas.ActualWidth / DrawingResources.ROOM_WIDTH;
            InnerBorder.Width = Math.Ceiling(deviceSize);
            InnerBorder.Height = Math.Ceiling(deviceSize);


            Canvas.SetLeft(DeviceNameLabel, Canvas.GetLeft(DeviceNameLabel) - 10);
            DeviceNameLabel.Width = deviceSize + 20;
            InnerBorder.Margin = new Thickness(0);

        }

        public void formatForStackPanel()
        {
            InnerBorder.Width = 64;
            InnerBorder.Height = 64;

            DeviceNameLabel.Width = this.Width;
            Canvas.SetLeft(DeviceNameLabel, 0);
            InnerBorder.Margin = new Thickness(18,0,0,0);
        }

        public DeviceControl(PairableDevice pairableDevice)
        {
            InitializeComponent();

            //Setup Events
            pairableDevice.LocationChanged += onLocationChanged;
            pairableDevice.OrientationChanged += onOrientationChanged;
            pairableDevice.PairingStateChanged += onPairingStateChanged;

            //Setup Display
            DeviceNameLabel.Content = pairableDevice.Identifier;
            InnerBorder.BorderBrush = DrawingResources.unpairedBrush;
            MyDisplayState = DisplayState.UnpairedAndOnStackPanel;

            formatForStackPanel();

        }

        public void onOrientationChanged(Device device)
        {


        }

        public void onLocationChanged(Device device)
        {
            PairableDevice pairableDevice = (PairableDevice)device;
            if (pairableDevice.PairingState == PairingState.Paired)
            {
                if (pairableDevice.Location.HasValue)
                {
                    Point newPoint = DrawingResources.ConvertFromMetersToDisplayCoordinates(pairableDevice.Location.Value, MainWindow.SharedCanvas);
                    Canvas.SetLeft(this, newPoint.X);
                    Canvas.SetTop(this, newPoint.Y);
                }
            }
            else 
            {
 
            }

        }

        public void onPairingStateChanged(PairableDevice pairableDevice)
        {
            //Dispatch UI Changes to Main Thread
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //Set device border to appropriate colour
                InnerBorder.BorderBrush = DrawingResources.GetBrushFromPairingState(pairableDevice.PairingState);

                //Set the control's owner
                if (pairableDevice.PairingState == PairingState.Paired)
                {
                    // When paired, we move the device to the canvas.
                    this.MyDisplayState = DisplayState.PairedAndOnCanvas;
                }
                else 
                {   
                    // If we are not paired or in pairing attempt, we go to stackpanel
                    this.MyDisplayState = DisplayState.UnpairedAndOnStackPanel;
                }

            }));

        }
    }
}
