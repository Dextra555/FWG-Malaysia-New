namespace OBMS.WebAPI.Utility
{
    public class Utility
    {

        public static List<string> GetStateList()
        {
            List<string> StateList = new List<string>();
            StateList.Add("Johor");
            StateList.Add("Kedah");
            StateList.Add("Kuala Lumpur");
            StateList.Add("Kelantan");
            StateList.Add("Labuan");
            StateList.Add("Melaka");
            StateList.Add("Negeri Sembilan");
            StateList.Add("Pahang");
            StateList.Add("Perak");
            StateList.Add("Perlis");
            StateList.Add("Pulau Pinang");
            StateList.Add("PutraJaya");
            StateList.Add("Sabah");
            StateList.Add("Sarawak");
            StateList.Add("Selangor");
            StateList.Add("Terengganu");
            StateList.Add("Wilayah Persekutuan");
            return StateList;
        }
        public static List<string> GetNationalityList()
        {
            List<string> NationalityList = new List<string>();
            NationalityList.Add("Nepal");
            return NationalityList;
        }
        public static List<string> GetICColorList()
        {
            List<string> ICColorList = new List<string>();
            ICColorList.Add("Not Applicable");
            ICColorList.Add("Blue");
            return ICColorList;
        }
        public static List<string> GetRaceList()
        {
            List<string> RaceList = new List<string>();
            RaceList.Add("Chinese");
            RaceList.Add("Indian");
            RaceList.Add("Malay");
            RaceList.Add("Others");
            return RaceList;

        }

        public static Dictionary<string, string> GetCompnay()
        {
            var results = new Dictionary<string, string>();

            results.Add("ShortName", "FWG");

            return results;
        }
    }
}
