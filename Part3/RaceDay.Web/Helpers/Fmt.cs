using System.Globalization;

namespace RaceDay.Web.Helpers
{
    /// <summary>Formatting helpers used by the views, so every page shows things the same way.</summary>
    public static class Fmt
    {
        /// <summary>47 becomes "47th", 1 becomes "1st", 12 becomes "12th".</summary>
        public static string Ordinal(int number)
        {
            var suffix = "th";
            var lastTwo = number % 100;
            if (lastTwo < 11 || lastTwo > 13)
            {
                switch (number % 10)
                {
                    case 1: suffix = "st"; break;
                    case 2: suffix = "nd"; break;
                    case 3: suffix = "rd"; break;
                }
            }
            return number + suffix;
        }

        /// <summary>A finish time as hh:mm:ss.</summary>
        public static string Time(TimeSpan? time)
        {
            if (time is null) return "-";
            var t = time.Value;
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        public static string Date(DateTime date) => date.ToString("ddd, dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

        public static string ShortDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        public static string TypeIcon(string? type) => type switch
        {
            "Run" => "🏃",
            "Walk" => "🚶",
            "Cycle" => "🚴",
            _ => "🏁"
        };

        /// <summary>The CSS class that colours an enrolment status: green, amber or red.</summary>
        public static string StatusClass(string? status) => status switch
        {
            "Confirmed" => "badge-confirmed",
            "Pending" => "badge-pending",
            "Cancelled" => "badge-cancelled",
            _ => "bg-secondary"
        };

        public static string AgeRange(int? min, int? max)
        {
            if (min is null && max is null) return "All ages";
            if (min is not null && max is not null) return $"{min}-{max} years";
            if (min is not null) return $"{min}+ years";
            return $"Up to {max} years";
        }
    
        /// <summary>Address the BROWSER uses to reach the API's static files (set in Program.cs).</summary>
        public static string PublicApiBase { get; set; } = "http://localhost:5254/";

        /// <summary>Images saved locally come back as /uploads/...; Azure ones are already full URLs.</summary>
        public static string ImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
            return PublicApiBase.TrimEnd('/') + "/" + url.TrimStart('/');
        }
}
}
