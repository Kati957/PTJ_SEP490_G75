using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.News
{
    public class NewsUpdateDto : NewsCreateDto
    {
        public int NewsID { get; set; }
    }
}
