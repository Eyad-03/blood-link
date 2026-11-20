using System.Globalization;
using BloodLink.Data;
using BloodLink.Models.Entities;

namespace BloodLink.Helpers
{
    // Small helpers that turn database values into the text the JavaScript pages expect.
    public static class Format
    {
        // BloodType.APositive -> "A+"   (null -> "")
        public static string Blood(BloodType? type)
        {
            return type == null ? "" : BloodTypeConverter.ToText(type.Value);
        }

        // "A+" -> BloodType.APositive   ("Not sure" or anything unknown -> null)
        public static BloodType? ParseBlood(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Trim().Replace("−", "-");   // the pages sometimes use the "−" sign
            string[] all = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            if (!all.Contains(text)) return null;
            return BloodTypeConverter.FromText(text);
        }

        // DateOnly -> "2026-10-10"
        public static string Date(DateOnly? date)
        {
            return date == null ? "" : date.Value.ToString("yyyy-MM-dd");
        }

        // TimeOnly -> "09:30"
        public static string Time(TimeOnly? time)
        {
            return time == null ? "" : time.Value.ToString("HH:mm");
        }

        public static DateOnly? ParseDate(string? text)
        {
            if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var d)) return d;
            return null;
        }

        public static TimeOnly? ParseTime(string? text)
        {
            if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, out var t)) return t;
            return null;
        }

        // DateTime (UTC) -> ISO text the browser understands: "2026-10-10T10:42:00.000Z"
        public static string Iso(DateTime? value)
        {
            if (value == null) return "";
            return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToString("o");
        }

        // "Lina Khalil" -> "LK"
        public static string Initials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            string result = parts[0][0].ToString();
            if (parts.Length > 1) result += parts[^1][0];
            return result.ToUpper();
        }

        // "Today, 10:42 AM" / "Yesterday, 4:15 PM" / "Oct 7, 2026"
        public static string Friendly(DateTime utc)
        {
            var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            var en = CultureInfo.GetCultureInfo("en-US");
            if (local.Date == DateTime.Now.Date) return "Today, " + local.ToString("h:mm tt", en);
            if (local.Date == DateTime.Now.Date.AddDays(-1)) return "Yesterday, " + local.ToString("h:mm tt", en);
            return local.ToString("MMM d, yyyy", en);
        }

        // OrganizationType.BloodDonationCenter -> "Blood donation center"
        public static string OrgType(OrganizationType type)
        {
            if (type == OrganizationType.BloodDonationCenter) return "Blood donation center";
            if (type == OrganizationType.MedicalCenter) return "Medical center";
            return "Hospital";
        }

        // Rough distance in km between two map points (Haversine formula).
        public static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLng = (lng2 - lng1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return Math.Round(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 1);
        }

        // Where is this user on the map?
        //   1. the exact point, if the donor chose to share it
        //   2. otherwise the centre of the user's city
        //   3. otherwise null (we don't know)
        public static (double Lat, double Lng)? PointOf(User user)
        {
            if (user.ShareLocation && user.Latitude != null && user.Longitude != null)
                return (user.Latitude.Value, user.Longitude.Value);
            if (user.City != null && Cities.ContainsKey(user.City))
                return Cities[user.City];
            return null;
        }

        // Distance text between two users, e.g. "2.4 km away" ("" when one location is unknown).
        public static string DistanceText(User a, User b)
        {
            var p1 = PointOf(a);
            var p2 = PointOf(b);
            if (p1 == null || p2 == null) return "";
            return DistanceKm(p1.Value.Lat, p1.Value.Lng, p2.Value.Lat, p2.Value.Lng) + " km away";
        }

        // Centre of each city, used when a user has no exact location on the map.
        public static readonly Dictionary<string, (double Lat, double Lng)> Cities = new()
        {
            ["Irbid"] = (32.5556, 35.8500),
            ["Amman"] = (31.9539, 35.9106),
            ["Zarqa"] = (32.0728, 36.0880),
            ["Aqaba"] = (29.5321, 35.0063),
            ["Salt"] = (32.0392, 35.7272),
            ["Madaba"] = (31.7160, 35.7939),
            ["Jerash"] = (32.2747, 35.8961),
            ["Ajloun"] = (32.3326, 35.7517),
            ["Mafraq"] = (32.3434, 36.2080),
            ["Karak"] = (31.1853, 35.7048),
            ["Tafilah"] = (30.8375, 35.6044),
            ["Ma'an"] = (30.1962, 35.7341)
        };
    }
}
