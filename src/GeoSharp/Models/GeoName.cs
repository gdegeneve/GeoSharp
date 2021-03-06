﻿using System;
using System.Collections.Generic;

namespace GeoSharp
{
    //http://www.geonames.org/export/codes.html
    public enum GeoFeatureClass
    {
        Country,                //A
        City,                   //P
        WaterBody,              //H
        LandArea,               //L
        TransportRoute,         //R
        Facility,               //S
        GeographicLandmark,     //T
        UnderseaLandmark,       //U
        Vegetation              //V
    }

    public enum GeoFeatureCode
    {
    }

    public enum GeoFields
    {
        GeoNameID,          //integer id of record in geonames database
        Name,               //name of geographical point (utf8) varchar(200)
        ASCIIName,          //name of geographical point in plain ascii characters, varchar(200)
        AlternateNames,     //alternatenames, comma separated, ascii names automatically transliterated, convenience attribute from alternatename table, varchar(8000)
        Latitude,           //latitude in decimal degrees (wgs84)
        Longitude,          //longitude in decimal degrees (wgs84)
        FeatureClass,       //see http://www.geonames.org/export/codes.html, char(1)
        FeatureCode,        //see http://www.geonames.org/export/codes.html, varchar(10)
        CountryCode,        //ISO-3166 2-letter country code, 2 characters
        AltCountryCodes,    //alternate country codes, comma separated, ISO-3166 2-letter country code, 60 characters
        Admin1Code,         //fipscode (subject to change to iso code), see exceptions below, see file admin1Codes.txt for display names of this code; varchar(20)
        Admin2Code,         //code for the second administrative division, a county in the US, see file admin2Codes.txt; varchar(80)
        Admin3Code,         //code for third level administrative division, varchar(20)
        Admin4Code,         //code for fourth level administrative division, varchar(20)
        Population,         //bigint (8 byte int)
        Elevation,          //in meters, integer
        DEM,                //digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer. srtm processed by cgiar/ciat.
        TimeZone,           //the timezone id (see file timeZone.txt) varchar(40)
        ModificationDate,   //date of last modification in yyyy-MM-dd format
        Count
    }

    public class GeoName : KDTree.IKDComparator<GeoName>
    {
        public string CountryCode { get; set; }
        public GeoFeatureClass FeatureClass { get; set; }
        public CountryInfo Country { get; set; }

        //ISO-3166 2 letter code
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Name { get; set; }

        public GeoName(string geoEntry)
        {
            string[] Fields = geoEntry.Split('\t');
            if (Fields.Length != (int)GeoFields.Count)
                throw new ArgumentException("Invalid GeoName Record");

            this.Name = Fields[1];
            this.CountryCode = Fields[8];
            this.FeatureClass = CodeToClass(Fields[6]);
            this.Latitude = double.Parse(Fields[4]);
            this.Longitude = double.Parse(Fields[5]);
        }

        public GeoName(string geoEntry, List<CountryInfo> countryInfos) : this(geoEntry)
        {
            if (countryInfos == null) throw new ArgumentNullException("countryInfos");
            this.Country = countryInfos.Find(s => s.Equals(CountryCode));
        }

        public GeoName(double latitude, double longitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public static char ClassToCode(GeoFeatureClass featureClass)
        {
            switch (featureClass)
            {
                case GeoFeatureClass.Country: return 'A';
                case GeoFeatureClass.City: return 'P';
                case GeoFeatureClass.WaterBody: return 'H';
                case GeoFeatureClass.LandArea: return 'L';
                case GeoFeatureClass.TransportRoute: return 'R';
                case GeoFeatureClass.Facility: return 'S';
                case GeoFeatureClass.GeographicLandmark: return 'T';
                case GeoFeatureClass.UnderseaLandmark: return 'U';
                case GeoFeatureClass.Vegetation: return 'V';
            }

            throw new ArgumentException("Invalid Feature Class");
        }

        public static GeoFeatureClass CodeToClass(string classCode)
        {
            if (classCode.Length != 1)
                throw new ArgumentException("Invalid Feature Class");

            switch (classCode[0])
            {
                case 'A': return GeoFeatureClass.Country;
                case 'P': return GeoFeatureClass.City;
                case 'H': return GeoFeatureClass.WaterBody;
                case 'L': return GeoFeatureClass.LandArea;
                case 'R': return GeoFeatureClass.TransportRoute;
                case 'S': return GeoFeatureClass.Facility;
                case 'T': return GeoFeatureClass.GeographicLandmark;
                case 'U': return GeoFeatureClass.UnderseaLandmark;
                case 'V': return GeoFeatureClass.Vegetation;
            }

            throw new ArgumentException("Invalid Feature Class");
        }

        public double AxisSquaredDistance(GeoName location, KDTree.Axis axis)
        {
            double Distance;
            if (axis == KDTree.Axis.X)
                Distance = GetX() - location.GetX();
            else if (axis == KDTree.Axis.Y)
                Distance = GetY() - location.GetY();
            else
                Distance = GetZ() - location.GetZ();

            return Distance * Distance;
        }

        public IComparer<GeoName> Comparator(KDTree.Axis axis)
        {
            switch (axis)
            {
                case KDTree.Axis.X: return new CompareX();
                case KDTree.Axis.Y: return new CompareY();
                case KDTree.Axis.Z: return new CompareZ();
            }

            throw new ArgumentException("Invalid Axis");
        }

        // The following methods are used purely for the KD-Tree
        // They don't convert lat/lon to any particular coordinate system
        public double GetX()
        {
            return Math.Cos(Deg2Rad(Latitude)) * Math.Cos(Deg2Rad(Longitude));
        }

        public double GetY()
        {
            return Math.Cos(Deg2Rad(Latitude)) * Math.Sin(Deg2Rad(Longitude));
        }

        public double GetZ()
        {
            return Math.Sin(Deg2Rad(Latitude));
        }

        public double SquaredDistance(GeoName location)
        {
            double x = GetX() - location.GetX();
            double y = GetY() - location.GetY();
            double z = GetZ() - location.GetZ();
            return (x * x) + (y * y) + (z * z);
        }

        public override string ToString()
        {
            return Name;
        }

        private double Deg2Rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        internal class CompareX : IComparer<GeoName>
        {
            public int Compare(GeoName x, GeoName y)
            {
                return x.GetX().CompareTo(y.GetX());
            }
        }

        internal class CompareY : IComparer<GeoName>
        {
            public int Compare(GeoName x, GeoName y)
            {
                return x.GetY().CompareTo(y.GetY());
            }
        }

        internal class CompareZ : IComparer<GeoName>
        {
            public int Compare(GeoName x, GeoName y)
            {
                return x.GetZ().CompareTo(y.GetZ());
            }
        }
    }
}