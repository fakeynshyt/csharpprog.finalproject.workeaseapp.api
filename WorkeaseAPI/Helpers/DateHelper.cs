namespace WorkeaseAPI.Helpers
{
    public static class DateHelper
    {
        public static DateTime GetEndOfMonth(int month, int year)
        {
            var lastDay = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, lastDay, 23, 59, 59);
        }
    }
}
