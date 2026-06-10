namespace WorkeaseAPI.Models
{
    public class Growth
    {
        // ── No separate PK — ChildId is the unique identifier ─────
        public int ChildId { get; set; }
        public Child? Child { get; set; }

        // ── Growth Categories ─────────────────────────────────────
        // Each category value = points spent (0 to max points)

        // Language
        public int Reading { get; set; } = 0;

        // Problem Solving
        public int Cognitive { get; set; } = 0;

        // Coordination
        public int Motor { get; set; } = 0;

        // Emotional
        public int Social { get; set; } = 0;

        // Expression
        public int Creative { get; set; } = 0;

        // Independence
        public int LifeSkills { get; set; } = 0;

        // ── Points ───────────────────────────────────────────────
        // TotalPoints = sum of all paid fee months × 75
        // AvailablePoints = TotalPoints - points already spent
        public int TotalPoints { get; set; } = 0;
        public int SpentPoints { get; set; } = 0;

        // ── Timestamps ───────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
