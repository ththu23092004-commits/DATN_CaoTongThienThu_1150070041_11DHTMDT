namespace VeSuKienWeb.Models
{
    public class SuKienChuDe
    {
        public int SuKienId { get; set; }
        public int ChuDeId { get; set; }

        public SuKien? SuKien { get; set; }
        public ChuDeSuKien? ChuDeSuKien { get; set; }
    }
}
