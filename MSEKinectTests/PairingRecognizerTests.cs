﻿using MSEKinect;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MSELocator;

namespace MSEKinectTests
{
    
    
    /// <summary>
    ///This is a test class for RoomTest and is intended
    ///to contain all RoomTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PairingRecognizerTests
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        internal PairablePerson CreateTestPerson(int newId, int? holdsId = null, PairingState? pairState = null)
        {
            PairablePerson person = new PairablePerson
            {
                Identifier = newId.ToString(),
                HeldDeviceIdentifier = holdsId.ToString() ?? null,
                Location = null,
                Orientation = null,
                PairingState = pairState ?? PairingState.NotPaired
            };

            return person;
        }

        internal PairableDevice CreateTestDevice(int newId, int? heldById = null, PairingState? pairState = null)
        {
            PairableDevice device = new PairableDevice
            {
                Identifier = newId.ToString(),
                HeldByPersonIdentifier = heldById.ToString() ?? null,
                PairingState = pairState ?? PairingState.NotPaired
            };

            return device;
        }


        /// <summary>
        ///A test for AttemptPairing
        ///</summary>
        [TestMethod()]
        public void AttemptPairingTest()
        {
            PairingRecognizer target = new PairingRecognizer(null, null); 

            List<PairableDevice> devices = new List<PairableDevice>(); 
            devices.Add(CreateTestDevice(1, null, PairingState.PairingAttempt)); 

            List<PairablePerson> persons = new List<PairablePerson>();
            persons.Add(CreateTestPerson(2,null, PairingState.PairingAttempt));

            bool expected = true;

            bool actual = target.AttemptPairing(devices, persons);

            Assert.AreEqual(expected, actual);

            Assert.IsTrue(devices[0].PairingState == PairingState.Paired);
            Assert.IsTrue(persons[0].PairingState == PairingState.Paired); 
        }

        /// <summary>
        ///A test for AttemptPairing
        ///</summary>
        [TestMethod()]
        public void AttemptPairingTestSingleState()
        {
            PairingRecognizer target = new PairingRecognizer(null, null); 

            List<PairableDevice> devices = new List<PairableDevice>();
            devices.Add(CreateTestDevice(1, null, PairingState.PairingAttempt));

            List<PairablePerson> persons = new List<PairablePerson>();
            persons.Add(CreateTestPerson(2, null, PairingState.NotPaired));

            bool expected = false;

            bool actual = target.AttemptPairing(devices, persons);

            Assert.AreEqual(expected, actual);

            Assert.IsTrue(devices[0].PairingState == PairingState.PairingAttempt);
            Assert.IsTrue(persons[0].PairingState == PairingState.NotPaired);
        }

        /// <summary>
        ///A test for AttemptPairing
        ///</summary>
        [TestMethod()]
        public void AttemptPairingWithPairedTest()
        {
            PairingRecognizer target = new PairingRecognizer(null, null);

            List<PairableDevice> devices = new List<PairableDevice>();
            devices.Add(CreateTestDevice(1, null, PairingState.PairingAttempt));

            List<PairablePerson> persons = new List<PairablePerson>();
            persons.Add(CreateTestPerson(2, null, PairingState.Paired));

            bool expected = false;

            bool actual = target.AttemptPairing(devices, persons);

            Assert.AreEqual(expected, actual);

            Assert.IsTrue(devices[0].PairingState == PairingState.PairingAttempt);
            Assert.IsTrue(persons[0].PairingState == PairingState.Paired);
        }
    }
}
