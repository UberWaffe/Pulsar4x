using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.Helpers.SIValues
{
    public class SiAngle
    {
        private double _angleRadians;

        public AngleEngUnits SiUnit { get { return AngleEngUnits.Radians; } }

        public SiAngle(double value, AngleEngUnits units)
        {
            _angleRadians = Conversion(value, units);
        }

        public double GetRadians()
        {
            return _angleRadians;
        }

        public double GetDegrees()
        {
            return Angle.ToDegrees(_angleRadians);
        }

        private double Conversion(double value, AngleEngUnits inputUnits)
        {
            switch (inputUnits)
            {
                case AngleEngUnits.Radians:
                    return value;
                case AngleEngUnits.Degrees:
                    return Angle.ToRadians(value);
                default:
                    throw new Exception("SiAngle -> Conversion invalid SI units provided");
            }
        }
    }



    public enum AngleEngUnits
    {
        Degrees,
        Radians
    }
}
