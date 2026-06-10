namespace WorkeaseAPI.Helpers
{
    public static class ReceiptGenerator
    {
        // Generates: WE-202504-00001
        // Format: WE-{YYYYMM}-{5 digit sequence}
        public static string Generate(int month, int year, int feeId)
        {
            var monthYear = $"{year}{month:D2}";
            var sequence = feeId.ToString().PadLeft(5, '0');
            return $"WE-{monthYear}-{sequence}";
        }

        // Generates unique receipt using GUID for extra uniqueness
        // Format: WE-202504-A1B2C
        public static string GenerateUnique(int month, int year)
        {
            var monthYear = $"{year}{month:D2}";
            var unique = Guid.NewGuid().ToString("N")[..5].ToUpper();
            return $"WE-{monthYear}-{unique}";
        }
    }
}
