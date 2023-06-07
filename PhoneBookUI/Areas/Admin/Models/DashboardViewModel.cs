namespace PhoneBookUI.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public Dictionary<string,int> PhoneTypePieData { get; set; }

        public string[] Labels { get; set; }
        public int[] Points { get; set; }
    }
}
