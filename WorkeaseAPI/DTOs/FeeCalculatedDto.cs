namespace WorkeaseAPI.DTOs
{
    public class FeeCalculatedDto
    {
        public decimal FeeTotalAmountPaid { get; set; }
        public decimal FeeTotalAmountOverdue { get; set; }
        public decimal FeeTotalRemainingAmount { get; set; }
    }
}
