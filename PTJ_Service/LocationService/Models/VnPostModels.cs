namespace PTJ_Service.LocationService.Models
    {
    //  PROVINCE 
    public class VnPostProvince
        {
        public int code { get; set; }
        public string name { get; set; }
        }

    // Response khi depth=2
    public class ProvinceWithDistricts
        {
        public int code { get; set; }
        public string name { get; set; }
        public List<VnPostDistrict> districts { get; set; }
        }

    //  DISTRICT 
    public class VnPostDistrict
        {
        public int code { get; set; }
        public string name { get; set; }
        }

    public class DistrictWithWards
        {
        public int code { get; set; }
        public string name { get; set; }
        public List<VnPostWard> wards { get; set; }
        }

    //  WARD 
    public class VnPostWard
        {
        public int code { get; set; }
        public string name { get; set; }
        }
    }
