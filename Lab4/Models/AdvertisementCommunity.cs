using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lab4.Models
{
    public class AdvertisementCommunity
    {
        public int ID { get; set; }
        public int AdvertisementID { get; set; }
        public Advertisement Advertisement { get; set; }
        public string CommunityID { get; set; }
        public Community Community { get; set; }
    }
}
