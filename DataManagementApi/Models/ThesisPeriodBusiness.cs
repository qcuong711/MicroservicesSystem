using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    // Bảng liên kết N-N giữa ThesisPeriod và Business
    [Table("ThesisPeriodBusiness")]
    public class ThesisPeriodBusiness
    {
        public int ThesisPeriodId { get; set; }
        public ThesisPeriod ThesisPeriod { get; set; } = null!;
        public int BusinessId { get; set; }
        public Business Business { get; set; } = null!;
    }
}
