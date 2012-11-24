﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSEGestureRecognizer;
using IntAirAct;
using MSELocator; 

namespace MSEKinect
{
    public class MSEKinectManager
    {
        PersonManager personManager;
        DeviceManager deviceManager;
        GestureController gestureController; 
        PairingRecognizer pairingRecognizer;
        IAIntAirAct intAirAct;
        CommunicationManager communicationManager;

        private LocatorInterface locator;
        public LocatorInterface Locator
        {
            get{ return locator; }
        }

        public PersonManager PersonManager
        {
            get { return personManager; }
        }

        public DeviceManager DeviceManager
        {
            get { return deviceManager; }
        }


        public void Start()
        {
            intAirAct = IAIntAirAct.New();

            locator = new Locator();

            //Instantiate Components 
            pairingRecognizer = new PairingRecognizer(locator, intAirAct);

            gestureController = new GestureController();
            personManager = new PersonManager(locator, gestureController, intAirAct);
            deviceManager = new DeviceManager(locator, intAirAct);


            personManager.StartPersonManager();
            deviceManager.StartDeviceManager();

            intAirAct.Start();

            gestureController.GestureRecognized += pairingRecognizer.PersonPairAttempt;

            communicationManager = new CommunicationManager(intAirAct, pairingRecognizer, gestureController, locator, personManager);
            
        }

        public void Stop()
        {
            personManager.StopPersonManager();
            deviceManager.StopDeviceManager();
            intAirAct.Stop();
        }


    }
}
