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
using System.Timers;
using System.Windows.Threading;
using IntAirAct;
using MSEAPI_SharedNetworking;
using MSEKinect;
using MSELocator;
using KinectServer;

namespace RoomVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Shared accessor for the window's canvas object.
        /// </summary>

        private static Canvas sharedCanvas;
        public static Canvas SharedCanvas
        {
            get { return sharedCanvas; }
        }

        private static WrapPanel kinectWrapPanel;
        public static WrapPanel KinectWrapPanel
        {
            get { return kinectWrapPanel; }
        }

        private static WrapPanel sharedWrapPanel;
        public static WrapPanel SharedDeviceStackPanel
        {
            get { return sharedWrapPanel; }
        }
        
        private static WrapPanel surfaceWrapPanel;
        public static WrapPanel SurfaceWrapPanel
        {
            get { return surfaceWrapPanel; }
        }       

        private static Border ghostBorder;
        public static Border GhostBorder
        {
            get { return ghostBorder; }
        }

        private static TextBlock ghostText;
        public static TextBlock GhostTextBlock
        {
            get { return ghostText; }
        }

        #region Instance Variables

        MSEKinectManager kinectManager;
        //DispatcherTimer dispatchTimer;

        /// <summary>
        /// Rendering code from the SkeletonBasics example, for demonstration purposes 
        /// </summary>
        private SkeletonRenderer skeletonRenderer;

        private Dictionary<PairablePerson, PersonControl> PersonControlDictionary;
        private Dictionary<string, DeviceControl> DeviceControlDictionary;
        private Dictionary<string, TrackerControl> TrackerControlDictionary;
        

        #endregion

        #region constants

        ///// <summary>
        ///// Width of output drawing
        ///// </summary>
        //private const float RenderWidth = 640.0f;

        ///// <summary>
        ///// Height of our output drawing
        ///// </summary>
        //private const float RenderHeight = 640.0f;

        //const double deviceDrawWidth = 0.25 * RenderWidth / ROOM_WIDTH;
        //const double deviceDrawHeight = 0.25 * RenderHeight / ROOM_HEIGHT;

        //const double trackerDrawWidth = 0.10 * RenderWidth / ROOM_WIDTH;
        //const double trackerDrawHeight = 0.10 * RenderHeight / ROOM_HEIGHT;

        //const int FPS = 60;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            sharedCanvas = canvas;
            sharedWrapPanel = unpairedDeviceStackPanel;
            surfaceWrapPanel = surfaceStackPanel;
            kinectWrapPanel = availableKinectsStackPanel;

            ghostBorder = ghost;
            ghostText = ghostTextBlock;

        }

        #region Drag and Drop

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            Point mouseLocation = e.GetPosition(sharedCanvas);
            Point canvasBounds = new Point(DrawingResources.ConvertFromMetersToPixelsX(DrawingResources.ROOM_WIDTH, sharedCanvas), DrawingResources.ConvertFromMetersToPixelsY(DrawingResources.ROOM_HEIGHT, sharedCanvas));

            // If the Cursor is within the Canvas
            if (mouseLocation.X < canvasBounds.X && mouseLocation.Y < canvasBounds.Y)
            {
                // Ensure the Ghost and Text are Visible while dragging
                MainWindow.GhostBorder.Visibility = System.Windows.Visibility.Visible;
                ghostTextBlock.Visibility = System.Windows.Visibility.Visible;

                // Relocate Ghost
                Canvas.SetLeft(MainWindow.GhostBorder, mouseLocation.X - (MainWindow.GhostBorder.Width / 2));
                Canvas.SetTop(MainWindow.GhostBorder, mouseLocation.Y - (MainWindow.GhostBorder.Height / 2));

                // Update Location Text
                Point p = DrawingResources.ConvertFromDisplayCoordinatesToMeters(mouseLocation, sharedCanvas);
                ghostTextBlock.Text = "(" + Math.Round(p.X,1) + ", " + Math.Round(p.Y,1) + ")";

                // Relocate Ghost Text
                Canvas.SetLeft(ghostTextBlock, mouseLocation.X - (MainWindow.GhostBorder.Width / 2));
                Canvas.SetTop(ghostTextBlock, mouseLocation.Y - (MainWindow.GhostBorder.Height / 2));

            }

            // If the Cursor is not within the Canvas
            else
            {
                // Hide the Ghost and Text because it would be drawn off the Canvas
                MainWindow.GhostBorder.Visibility = System.Windows.Visibility.Hidden;
                ghostTextBlock.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void cleanUpKinectPersons(string kinectID)
        {
            lock (kinectManager.Locator.threadLock)
            {
                foreach (KeyValuePair<PairablePerson, PersonControl> entry in PersonControlDictionary.ToList())
                {
                    if (entry.Key.TrackerIDwithSkeletonID.Keys.Contains(kinectID))
                    {
                        this.Dispatcher.Invoke(new Action(delegate()
                        {
                            canvas.Children.Remove(PersonControlDictionary[entry.Key]);
                            PersonControlDictionary.Remove(entry.Key);
                            kinectManager.Locator.Persons.Remove(entry.Key as Person);
                        }));
                    }
                }
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            string DataType = e.Data.GetFormats(true)[0];

            //if the object dropped is a tracker
            if (DataType == "trackerControl")
            {
                base.OnDrop(e);
                Point mouseLocation = e.GetPosition(sharedCanvas);

                // Grab the data we packed into the DataObject
                TrackerControl trackerControl = (TrackerControl)e.Data.GetData("trackerControl");

                // Hide the Ghost and Text since a Drop has been made
                MainWindow.GhostBorder.Visibility = System.Windows.Visibility.Hidden;
                ghostTextBlock.Visibility = System.Windows.Visibility.Hidden;

                // Return the Opacity of the TrackerControl
                trackerControl.Opacity = 1;

                trackerControl.Tracker.Location = DrawingResources.ConvertFromDisplayCoordinatesToMeters(mouseLocation, sharedCanvas);
                

                // Check if the TrackerControl is already a child of Shared Canvas
                Point canvasBounds = new Point(DrawingResources.ConvertFromMetersToPixelsX(DrawingResources.ROOM_WIDTH, sharedCanvas), DrawingResources.ConvertFromMetersToPixelsY(DrawingResources.ROOM_HEIGHT, sharedCanvas));   

                if (!trackerControl.IsDescendantOf(SharedCanvas))
                {
                    trackerControl.formatForCanvas();
                    kinectWrapPanel.Children.Remove(trackerControl);
                    SharedCanvas.Children.Add(trackerControl);

                    if (trackerControl.Tracker.Orientation == null)
                        trackerControl.Tracker.Orientation = 270;
                }

                // if the cursor is outside the canvas, put the tracker back in stackpanel.
                else if (!(mouseLocation.X < canvasBounds.X && mouseLocation.Y < canvasBounds.Y))
                {
                    trackerControl.Tracker.StopStreaming();
                    cleanUpKinectPersons(trackerControl.Tracker.Identifier);
                    trackerControl.formatForStackPanel();
                    SharedCanvas.Children.Remove(trackerControl);
                    kinectWrapPanel.Children.Add(trackerControl);
                }
            }
            
            //if the objet dropped is a device.
            else if (DataType == "deviceControl")
            {
                base.OnDrop(e);
                Point mouseLocation = e.GetPosition(sharedCanvas);

                // Grab the data we packed into the DataObject
                DeviceControl deviceControl = (DeviceControl)e.Data.GetData("deviceControl");

                // Hide the Ghost and Text since a Drop has been made
                MainWindow.GhostBorder.Visibility = System.Windows.Visibility.Hidden;
                ghostTextBlock.Visibility = System.Windows.Visibility.Hidden;

                // Return the Opacity of the DeviceControl
                deviceControl.Opacity = 1;

                PairableDevice device = deviceControl.PairableDevice;
                IADevice iaDevice = deviceControl.IADevice;

                IARequest request = new IARequest(Routes.SetLocationRoute);
                request.Parameters["identifier"] = iaDevice.Name;

                Point canvasBounds = new Point(DrawingResources.ConvertFromMetersToPixelsX(DrawingResources.ROOM_WIDTH, sharedCanvas), DrawingResources.ConvertFromMetersToPixelsY(DrawingResources.ROOM_HEIGHT, sharedCanvas));

                //if the dragged device is a pairable device (i.e iPad)
                if (!iaDevice.SupportedRoutes.Contains(Routes.GetLocationRoute))
                {
                    Point mouseLocationOnCanvas = mouseLocation = DrawingResources.ConvertFromDisplayCoordinatesToMeters(mouseLocation, sharedCanvas);
                    bool pairedToNewDevice = false;

                    foreach (KeyValuePair<PairablePerson, PersonControl> keyPair in PersonControlDictionary)
                    {
                        Point personLocation = keyPair.Key.Location.Value;
                        double distance = Math.Sqrt(Math.Pow(mouseLocationOnCanvas.X - personLocation.X, 2) + Math.Pow(mouseLocationOnCanvas.Y - personLocation.Y, 2));

                        //if the mouse drop is close to a person, pair the device with that person.
                        if (distance < 0.3 && (device.HeldByPersonIdentifier == keyPair.Key.Identifier || keyPair.Key.PairingState != PairingState.Paired))
                        {
                            if (device.PairingState == PairingState.Paired || device.PairingState == PairingState.PairedButOccluded)
                                kinectManager.PairingRecognizer.UnpairDevice(device);

                            kinectManager.PairingRecognizer.Pair(device, keyPair.Key);
                            pairedToNewDevice = true;
                            break;
                        }
                    }

                    //if the mouse drop is not close to a person then unpair the device.
                    if (!pairedToNewDevice)
                        kinectManager.PairingRecognizer.UnpairDevice(device);
                }

                //if the dragged device is not a pairable device (i.e table-top)
                else if (iaDevice.SupportedRoutes.Contains(Routes.GetLocationRoute))
                {
                    if (mouseLocation.X < canvasBounds.X && mouseLocation.Y < canvasBounds.Y)
                    {
                        // Dropped within Canvas, so we want to place it on the canvas
                        device.Location = DrawingResources.ConvertFromDisplayCoordinatesToMeters(mouseLocation, sharedCanvas);
                        request.SetBodyWith(new IntermediatePoint(device.Location.Value));
                    }
                    else
                    {
                        // Not dropped within Canvas, so we want to put it back on the stack panel
                        device.Location = null;
                        request.SetBodyWith(null);
                    }

                    // Send a request to the Device that their location has changed
                    kinectManager.IntAirAct.SendRequest(request, iaDevice);
                }
            }
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            this.Dispatcher.Invoke(new Action(delegate()
            {
              
        
            DrawingResources.GenerateGridLines(canvas, GridLines, GridLinesScaleSlider.Value);
            GridLines.ShowGridLines = true;

            // When we do the event handling through XAML, an event fires before the Window is loaded, and it freezes the program, so we do event binding after Window is loaded
            GridLinesScaleSlider.ValueChanged += UpdateGridlines;
            GridLinesCheckBox.Checked += ChangeGridlineVisibility;
            GridLinesCheckBox.Unchecked += ChangeGridlineVisibility;
            RangeCheckBox.Checked += ChangeRangeVisibility;
            RangeCheckBox.Unchecked += ChangeRangeVisibility;
                   
            //Create Dictionaries for DeviceControl, PersonControl
            DeviceControlDictionary = new Dictionary<string, DeviceControl>();
            PersonControlDictionary = new Dictionary<PairablePerson, PersonControl>();
            TrackerControlDictionary = new Dictionary<string, TrackerControl>();


            //Initialize and Start MSEKinectManager
            kinectManager = new MSEKinectManager();
            kinectManager.Start();

            // The tracker is created in the PersonManager constructor, so there's actually no way for us to listen for its creation the first time
            //trackerChanged(kinectManager.PersonManager, kinectManager.PersonManager.Tracker);

            //Setup Events for Device Addition and Removal, Person Addition and Removal 
            kinectManager.DeviceManager.DeviceAdded += deviceAdded;
            kinectManager.DeviceManager.DeviceRemoved += deviceRemoved;
            kinectManager.PersonManager.PersonAdded += personAdded;
            kinectManager.PersonManager.PersonRemoved += personRemoved;
            kinectManager.PersonManager.newKinectDiscovered += KinectDiscovered;
            kinectManager.PersonManager.kinectRemoved += KinectRemoved;

            //Seperate components for displaying the visible skeletons
            skeletonRenderer = new SkeletonRenderer(SkeletonBasicsImage);

            //// Values retrieved from:
            //// http://blogs.msdn.com/b/kinectforwindows/archive/2012/01/20/near-mode-what-it-is-and-isn-t.aspx
            //// http://msdn.microsoft.com/en-us/library/jj131033.aspx
            //tracker.MinRange = 0.8;
            //tracker.MaxRange = 4;
            //tracker.FieldOfView = 57;

            }));
        }

        private void KinectDiscovered(string kinectID, Point? KinectLocation, Double? KinectOrientation)
        {
            Tracker tracker = kinectManager.Locator.Trackers.Find(x => x.Identifier.Equals(kinectID));

            this.Dispatcher.Invoke(new Action(delegate()
            {
                //if the discovered kinect doesn't have a location, put the kinect in stack panel
                if (KinectLocation == null)
                {
                    TrackerControlDictionary[tracker.Identifier] = new TrackerControl(tracker);
                    TrackerControlDictionary[tracker.Identifier].formatForStackPanel();
                    availableKinectsStackPanel.Children.Add(TrackerControlDictionary[tracker.Identifier]);
                }
                else
                {
                    TrackerControlDictionary[tracker.Identifier] = new TrackerControl(tracker);
                    TrackerControlDictionary[tracker.Identifier].formatForCanvas();
                    canvas.Children.Add(TrackerControlDictionary[tracker.Identifier]);

                    tracker.Location = KinectLocation;

                    if (KinectOrientation != null)
                        tracker.Orientation = KinectOrientation;
                }
            }));
        }

        private void KinectRemoved(string KinectID)
        {

           this.Dispatcher.Invoke(new Action(delegate()
            {
                canvas.Children.Remove(TrackerControlDictionary[KinectID]);
                kinectWrapPanel.Children.Remove(TrackerControlDictionary[KinectID]);
                TrackerControlDictionary.Remove(KinectID);
           }));
        }

        //Window Close (End the Kinect Manager) 
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kinectManager.Stop();
        }  

        #region Handlers for Person and Device manager events
        
        void deviceAdded(DeviceManager deviceManager, PairableDevice pairableDevice)
        {
            // Finds the matching IADevice from the pairableDevice Identifier
            IADevice iaDevice = deviceManager.IntAirAct.Devices.Find(d => d.Name.Equals(pairableDevice.Identifier));

            if (iaDevice.SupportedRoutes.Contains(Routes.BecomePairedRoute))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeviceControlDictionary[pairableDevice.Identifier] = new DeviceControl(pairableDevice, iaDevice);
                    unpairedDeviceStackPanel.Children.Add(DeviceControlDictionary[pairableDevice.Identifier]);
                }));
            }
            else if (iaDevice.SupportedRoutes.Contains(Routes.GetLocationRoute))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    DeviceControlDictionary[pairableDevice.Identifier] = new DeviceControl(pairableDevice, iaDevice);
                    surfaceStackPanel.Children.Add(DeviceControlDictionary[pairableDevice.Identifier]);
                }));
            }
        }

        void deviceRemoved(DeviceManager deviceManager, PairableDevice pairableDevice)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DeviceControlDictionary.ContainsKey(pairableDevice.Identifier))
                {
                    canvas.Children.Remove(DeviceControlDictionary[pairableDevice.Identifier]);
                    unpairedDeviceStackPanel.Children.Remove(DeviceControlDictionary[pairableDevice.Identifier]);
                    surfaceStackPanel.Children.Remove(DeviceControlDictionary[pairableDevice.Identifier]);

                    DeviceControlDictionary.Remove(pairableDevice.Identifier);
                }
            }));

        }

        void personAdded(PersonManager personManager, PairablePerson pairablePerson)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (!PersonControlDictionary.ContainsKey(pairablePerson))
                {
                    PersonControlDictionary[pairablePerson] = new PersonControl(pairablePerson);
                    canvas.Children.Add(PersonControlDictionary[pairablePerson]);
                }
            }));
        }

        void personRemoved(PersonManager personManager, PairablePerson pairablePerson)
        {

            this.Dispatcher.Invoke(new Action(delegate()
            {
                if (PersonControlDictionary.ContainsKey(pairablePerson))
                {
                    canvas.Children.Remove(PersonControlDictionary[pairablePerson]);
                    PersonControlDictionary.Remove(pairablePerson);
                }
            }));
        }

        void trackerChanged(PersonManager sender, Tracker tracker)
        {
            if (tracker != null)
            {
                //drawnTracker = new DrawnTracker(tracker);
            }
        }

        void Clean()
        {
            
        }

        #endregion

        // Updates the scale of the Gridlines
        private void UpdateGridlines(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GridLinesScaleSlider.Value == 1)
                MetersTextBlock.Text = " meter";
            else
                MetersTextBlock.Text = " meters";

            DrawingResources.GenerateGridLines(canvas, GridLines, GridLinesScaleSlider.Value);
        }

        private void ChangeRangeVisibility(object sender, RoutedEventArgs e)
        {
            if (RangeCheckBox.IsChecked.HasValue && RangeCheckBox.IsChecked.Value == true)
            {
                // Show Range
                foreach (KeyValuePair<string,TrackerControl> pair in TrackerControlDictionary)
                {
                    pair.Value.showRange();
                }
            }
            else if (RangeCheckBox.IsChecked.HasValue && RangeCheckBox.IsChecked.Value == false)
            {
                // Hide Range
                foreach (KeyValuePair<string, TrackerControl> pair in TrackerControlDictionary)
                {
                    pair.Value.hideRange();
                }
            }
        }

        // Hides/Shows the Gridlines and Slider based on the Checkbox's state
        private void ChangeGridlineVisibility(object sender, RoutedEventArgs e)
        {
            if (GridLinesCheckBox.IsChecked.HasValue && GridLinesCheckBox.IsChecked.Value == true)
            {
                // Show Gridlines
                GridLines.ShowGridLines = true;
                GridLinesScaleSlider.Visibility = System.Windows.Visibility.Visible;
                GridLinesScaleStackPanel.Visibility = System.Windows.Visibility.Visible;

            }
            else if (GridLinesCheckBox.IsChecked.HasValue && GridLinesCheckBox.IsChecked.Value == false)
            {
                // Hide Gridlines
                GridLines.ShowGridLines = false;
                GridLinesScaleSlider.Visibility = System.Windows.Visibility.Collapsed;
                GridLinesScaleStackPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                kinectManager.Stop();

                Environment.Exit(0);
            }
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            kinectManager.PersonManager.resetPeople();

            foreach(PairablePerson pairablePerson in PersonControlDictionary.Keys.ToList())
            {
                this.Dispatcher.Invoke(new Action(delegate()
                {
                    canvas.Children.Remove(PersonControlDictionary[pairablePerson]);
                    PersonControlDictionary.Remove(pairablePerson);
                }));
            }
        }

        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
            kinectManager.PersonManager.calibrate();
        }


    }
}
