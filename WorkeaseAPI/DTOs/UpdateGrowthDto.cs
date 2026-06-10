using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class UpdateGrowthDto
    {
        public int Reading { get; set; }

        public int Cognitive { get; set; }

        public int Motor { get; set; }

        public int Social { get; set; }

        public int Creative { get; set; }

        public int LifeSkills { get; set; }
    }
}
