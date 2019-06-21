using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Orbit
{
    /// <summary>
    /// This is a very simplified representation of orbits.
    /// It simply assumes that the orbit is a perfect circle, centered on the parent object.
    /// So change in position is simply the time-delta multiplied by the angular velocity.
    /// </summary>
    public class SimpleOrbitProcessor
    {
        public Vector3 CalculatePositionChangeInReferenceToParent(Vector3 currentPosition, double orbitRadiusInKm, double orbitAngularVelocityInRadiansPerSeconds, double startingRelativeAngleInRadians, TimeSpan timeDelta)
        {
            var totalSecondsDelta = timeDelta.TotalSeconds;
            var totalAngleChange = orbitAngularVelocityInRadiansPerSeconds * totalSecondsDelta;
            var finalRelativeAngle = Angle.NormaliseRadians(startingRelativeAngleInRadians + totalAngleChange);

            var newPosition = currentPosition;


            return newPosition;
        }
    }
}
