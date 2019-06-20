using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SimpleOrbitDB
    {
        [PublicAPI]
        [JsonProperty]
        public double Radius { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public double AngularVelocity { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public double PreviousRelativeAngle { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public Guid ParentId { get; private set; }

        public SimpleOrbitDB()
        { }

        public SimpleOrbitDB(SimpleOrbitDB orbitToClone)
        {
            Radius = orbitToClone.Radius;
            AngularVelocity = orbitToClone.AngularVelocity;
            PreviousRelativeAngle = orbitToClone.PreviousRelativeAngle;
            ParentId = orbitToClone.ParentId;
        }
    }
}
