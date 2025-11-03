using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PTJ_Models.Models;
    public class Image
    {
        public int ImageID { get; set; }
        public string EntityType { get; set; } = null!;
        public int EntityID { get; set; }
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public string? Format { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey("EntityID")]
        public News? News { get; set; }
}

