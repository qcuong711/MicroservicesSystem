using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    [Table("PartnerBusiness")]
    public class PartnerBusiness
    {
        public int PartnerId { get; set; }
        public Partner Partner { get; set; } = null!;
        public int BusinessId { get; set; }
        public Business Business { get; set; } = null!;
    }
}
