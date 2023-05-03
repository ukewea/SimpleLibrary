namespace SimpleLibrary
{
    public class UserGymUsageDailyReportDTO
    {
        public DateTime Date { get; set; }

        public string GymIdNumber { get; set; }

        public DateTime? GymRecordTime { get; set; }

        public DateTime? SpeedGateRecordTime { get; set; }

        public bool IsWorkDay()
        {
            return Date.DayOfWeek != DayOfWeek.Sunday && 
                Date.DayOfWeek != DayOfWeek.Saturday;
        }
    }
}
