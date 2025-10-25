using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PostDTO
{
    public class SaveJobDto
    {
        public int JobSeekerId { get; set; }
        public int EmployerPostId { get; set; }
        public string? Note { get; set; }
    }
}
