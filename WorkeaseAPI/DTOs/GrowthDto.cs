namespace WorkeaseAPI.DTOs
{
    public class GrowthDto
    {
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;

        // ── Categories ────────────────────────────────────────────
        public int Reading { get; set; }
        public int Cognitive { get; set; }
        public int Motor { get; set; }
        public int Social { get; set; }
        public int Creative { get; set; }
        public int LifeSkills { get; set; }

        // ── Points ────────────────────────────────────────────────
        public int TotalPoints { get; set; }
        public int SpentPoints { get; set; }
        public int AvailablePoints => TotalPoints - SpentPoints;

        // ── Timestamps ────────────────────────────────────────────
        public DateTime UpdatedAt { get; set; }
    }

}
