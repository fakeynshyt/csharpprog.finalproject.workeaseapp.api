namespace WorkeaseAPI.Helpers
{
    public class ReportStats
    {
        public int TotalChildren { get; set; }
        public int RecordedCount { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AverageBmi { get; set; }
        public int UnderweightCount { get; set; }
        public int NormalCount { get; set; }
        public int OverweightCount { get; set; }
        public int PaidCount { get; set; }
        public int UnpaidCount { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
    }
}
