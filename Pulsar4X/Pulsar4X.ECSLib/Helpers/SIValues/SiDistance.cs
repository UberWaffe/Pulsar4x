using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.Helpers.SIValues
{
    public class SiDistance
    {
        private double _distanceInMeters;

        public DistanceEngUnits SiUnit { get { return DistanceEngUnits.Meters; } }

        public SiDistance(double value, DistanceEngUnits units)
        {
            _distanceInMeters = Conversion(value, units);
        }

        public double GetMeters()
        {
            return _distanceInMeters;
        }

        public double GetKilometers()
        {
            return Distance.MToKm(_distanceInMeters);
        }

        public double GetAU()
        {
            return Distance.MToAU(_distanceInMeters);
        }

        private double Conversion(double value, DistanceEngUnits inputUnits)
        {
            switch (inputUnits)
            {
                case DistanceEngUnits.Meters:
                    return value;
                case DistanceEngUnits.Kilometers:
                    return Distance.KmToM(value);
                case DistanceEngUnits.AstronomicUnits:
                    return Distance.AuToMt(value);
                default:
                    throw new Exception("SiDistance -> Conversion invalid SI units provided");
            }
        }
    }



    public enum DistanceEngUnits
    {
        Meters,
        Kilometers,
        AstronomicUnits
    }
}
