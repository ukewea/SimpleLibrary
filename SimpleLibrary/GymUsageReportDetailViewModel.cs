namespace SimpleLibrary
{
    public class GymUsageReportDetailViewModel
    {
        public string GymDate { get; set; }
        public string GymIdNumber { get; set; }
        public string GymUsageValidType =>
            GymUsageValidTypeEnum.HasValue ? GymUsageValidTypeEnum.GetDisplayName() : string.Empty;

        public GymUsageValidTypeEnum? GymUsageValidTypeEnum { get; set; } = null;
        public string GymRecordTime { get; set; }
        public string AccumulativeUsagePercentage { get; set; }
    }
}
