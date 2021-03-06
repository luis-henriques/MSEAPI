﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MSELocator
{ 
    
    public class Device
    {


        private double? _Orientation;
        public double? Orientation
        {
            get { return _Orientation; }
            set
            {
                if (value.HasValue)
                {
                    _Orientation = Util.NormalizeAngle(value.Value);
                }
                else
                {
                    _Orientation = null;
                }

                if (OrientationChanged != null)
                    OrientationChanged(this);
            }
        }

        private double? _FieldOfView;
        public double? FieldOfView
        {
            get { return _FieldOfView; }
            set 
            {
                if (value.HasValue)
                {
                    _FieldOfView = Util.NormalizeAngle(value.Value);
                }
                else
                {
                    _FieldOfView = null;
                }

                if (FOVChanged != null)
                    FOVChanged(this);
            
            }


        }

        private Point? _Location;
        public Point? Location
        {
            get { return _Location; }
            set 
            {       
                _Location = value;
                if (LocationChanged != null)
                    LocationChanged(this);
            }
        }

        private Double? _Height;
        public Double? Height
        {
            get { return _Height; }
            set
            {
                _Height = value;
            }
        }

        private Double? _Width;
        public Double? Width
        {
            get { return _Width; }
            set
            {
                _Width = value;
            }
        }

        private String _HeldByPersonIdentifier;
        public String HeldByPersonIdentifier
        {
            get { return _HeldByPersonIdentifier; }
            set { _HeldByPersonIdentifier = value; }
        } 

        private String _Identifier;
        public String Identifier
        {
            get
            {
                return _Identifier;
            }
            set
            {
                _Identifier = value;
            }
        }

        public Dictionary<string, double> intersectionPoint
        {
            get;
            set;
        }


        public delegate void DeviceEventSignature(Device sender);
        public event DeviceEventSignature LocationChanged;
        public event DeviceEventSignature OrientationChanged;
        public event DeviceEventSignature FOVChanged;

        /// <summary>
        /// This method overrrides the GetHashCode to resolve the warning raised when Equals
        /// is ovverrieden but GetHashCode isn't.
        /// </summary>
        /// <returns>Hash code of the object</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            Device d = obj as Device;
            
            if ((System.Object)d == null)
            {
                return false;
            }

            return (this.Identifier == d.Identifier);
        }


        public bool Equals(Device d)
        {
            return (d.Identifier == this.Identifier);
        }

        public override string ToString()
        {
            return String.Format("Device[Orientation: {0}, HeldByPersonIdentifier: {1}]", Orientation, HeldByPersonIdentifier);
        }




        public Device()
        {
            this.FieldOfView = Util.DEFAULT_FIELD_OF_VIEW;
            this.intersectionPoint = new Dictionary<string, double>();
            intersectionPoint.Add("x", 0);
            intersectionPoint.Add("y", 0);
        }







            
    }
}
