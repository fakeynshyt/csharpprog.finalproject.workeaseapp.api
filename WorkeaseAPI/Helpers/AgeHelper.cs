namespace WorkeaseAPI.Helpers
{
    public static class AgeHelper
    {
        // Calculates age in months from birthdate to today
        public static int GetAgeInMonths(DateTime birthDate)
        {
            var today = DateTime.Today;
            var months = ((today.Year - birthDate.Year) * 12)
                       + (today.Month - birthDate.Month);

            // If the day hasn't come yet this month, subtract 1
            if (today.Day < birthDate.Day)
                months--;

            return months < 0 ? 0 : months;
        }
    }
}
