﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy; 
using IntAirAct;
using Nancy.ModelBinding;

namespace MSEKinect.Modules
{
    public class OrientationModule: NancyModule
    {
        public OrientationModule(Room room)
        {
            Get["device/{identifier}"] = parameters =>
            {
                String name = Uri.UnescapeDataString(parameters.identifier);

                //Find the associated device in the Current Devices 
                Device device = room.CurrentDevices.Find(d => d.Identifier.Equals(name));

                return Response.RespondWith(device, "devices");
            };

            Put["device/{identifier}"] = parameters =>
            {
                Device d = this.Bind();

                Console.WriteLine(d.ToString());

                return "Hello " + parameters.identifier;
            };
        }
    }
}
