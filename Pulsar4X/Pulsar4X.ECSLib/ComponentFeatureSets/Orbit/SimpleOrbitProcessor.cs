using Pulsar4X.ECSLib.Helpers.SIValues;
using Pulsar4X.Vectors;
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
        public Vector3 CalculatePositionChangeInReferenceToParent(Vector3 currentPosition, SiDistance orbitRadiusInKm, SiAngle orbitAngularVelocityInRadiansPerSeconds, SiAngle startingRelativeAngleInRadians, TimeSpan timeDelta)
        {
            var totalSecondsDelta = timeDelta.TotalSeconds;
            var totalAngleChange = orbitAngularVelocityInRadiansPerSeconds.GetRadians() * totalSecondsDelta;
            var finalRelativeAngle = Angle.NormaliseRadians(startingRelativeAngleInRadians.GetRadians() + totalAngleChange);

            var circleCenter = CircleCalculations.GetCenterOfCircle(new Vector2(currentPosition.X, currentPosition.Z), orbitRadiusInKm, startingRelativeAngleInRadians);
            var newPointOnCircle = CircleCalculations.GetPositionOnCircle(circleCenter, orbitRadiusInKm, new SiAngle(finalRelativeAngle, AngleEngUnits.Radians));

            var newPosition = circleCenter + newPointOnCircle;

            return new Vector3(newPosition.X, newPosition.Y, currentPosition.Z);
        }
    }
}
