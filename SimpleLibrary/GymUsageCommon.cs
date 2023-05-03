namespace SimpleLibrary
{
    public class GymUsageCommon
    {
        public static int CalcBurden(decimal ratio, out string remark)
        {
            remark = "";
            switch (ratio)
            {
                case decimal n when n >= 59m:
                    return 0;
                case decimal n when n >= 44m && n < 59m:
                    return 404;
                case decimal n when n < 44m:
                    if (n == 0)
                    {
                        remark = "需調整健身房";
                    }
                    return 503;
                default:
                    return 0;
            }
        }

        public static decimal ProcessReportDetailAndCalcTotalGymUsage(
            DateTime startDate, DateTime endDate,
            DateTime? latestCreatedTime,
            List<GymPermit> empGymPermits,
            List<UserGymUsageDailyReportDTO> dailyReport,
            List<HolidayMakeupDay> specialDateList,
            out List<GymUsageReportDetailViewModel> detailList)
        {
            detailList = new List<GymUsageReportDetailViewModel>();

            var tempTotalGymUsage = 0m;
            var countGymRecord = 0;
            var countValidUsage = 0;

            // 補足日期
            FillMissingDays(dailyReport, startDate, endDate);

            foreach (var r in dailyReport.OrderBy(x => x.Date))
            {
                var detail = new GymUsageReportDetailViewModel
                {
                    GymIdNumber = r.GymIdNumber,
                    GymDate = r.Date.ToString("yyyy/MM/dd ddd")
                };

                // 判定該日是否為特殊日
                var (isHoliday, isMakeupDay) = DetermineDateType(r.Date, specialDateList);

                var isValidTime = IsValidTime(empGymPermits, r.Date);
                detail.GymUsageValidTypeEnum = GetGymUsageValidType(r, isValidTime, isHoliday, isMakeupDay, latestCreatedTime);
                if (detail.GymUsageValidTypeEnum == GymUsageValidTypeEnum.NO_RECORD)
                {
                    // 平日 無進健身房 無門禁 = 沒進公司
                    countGymRecord++;
                    countValidUsage++;
                }

                if (!detail.GymUsageValidTypeEnum.HasValue)
                {
                    // 除此之外，其餘需列入計算
                    if (r.GymRecordTime.HasValue || r.SpeedGateRecordTime.HasValue)
                    {
                        countValidUsage++;
                    }

                    if (r.GymRecordTime.HasValue)
                    {
                        // 有進健身房就算(從寬)
                        countGymRecord++;
                        detail.GymRecordTime = r.GymRecordTime.Value.ToString("yyyy/MM/dd HH:mm:ss");
                        detail.GymUsageValidTypeEnum = GymUsageValidTypeEnum.YES;
                    }
                    else if (!r.GymRecordTime.HasValue && r.SpeedGateRecordTime.HasValue)
                    {
                        detail.GymUsageValidTypeEnum = GymUsageValidTypeEnum.NO;
                    }
                }
                else
                {
                    // 補齊不列入計算之其餘資訊欄位
                    if (r.GymRecordTime.HasValue)
                    {
                        detail.GymRecordTime = r.GymRecordTime.Value.ToString("yyyy/MM/dd HH:mm:ss");
                    }
                }

                if (countValidUsage == 0)
                {
                    tempTotalGymUsage = 0;
                }
                else
                {
                    tempTotalGymUsage = Math.Round(
                        (decimal)countGymRecord / countValidUsage * 100,
                        1, MidpointRounding.AwayFromZero);
                }

                detail.AccumulativeUsagePercentage = tempTotalGymUsage + "%";
                detailList.Add(detail);
            }

            return tempTotalGymUsage;
        }

        public static void FillMissingDays(
            List<UserGymUsageDailyReportDTO> dailyReport,
            DateTime startDate, DateTime endDate)
        {
            HashSet<DateTime> existingDates = new(dailyReport.Select(x => x.Date));

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                if (!existingDates.Contains(d))
                {
                    dailyReport.Add(new UserGymUsageDailyReportDTO { Date = d });
                }
            }
        }

        public static (bool IsHoliday, bool IsMakeupDay) DetermineDateType(
            DateTime date, List<HolidayMakeupDay> specialDateList)
        {
            var specialDate = specialDateList.Find(x => x.Date == date);
            var isHoliday = false;
            var isMakeupDay = false;

            if (specialDate != null)
            {
                switch (specialDate.DateType)
                {
                    case SpecialEventType.NATIONAL_HOLIDAY:
                        isHoliday = true;
                        break;
                    case SpecialEventType.MAKEUP_DAY:
                        isMakeupDay = true;
                        break;
                }
            }

            return (IsHoliday: isHoliday, IsMakeupDay: isMakeupDay);
        }

        public static bool IsValidTime(List<GymPermit> empGymPermitList, DateTime checkDate)
        {
            bool isValidTime = false;
            empGymPermitList.ForEach(x =>
            {
                if (x.ValidTime <= checkDate && (!x.InvalidTime.HasValue || x.InvalidTime >= checkDate))
                {
                    isValidTime = true;
                    return;
                }
            });

            return isValidTime;
        }

        public static GymUsageValidTypeEnum? GetGymUsageValidType(
            UserGymUsageDailyReportDTO r,
            bool isValidTime, bool isHoliday, bool isMakeupDay,
            DateTime? latestCreatedTime)
        {
            if (!isValidTime)
            {
                return GymUsageValidTypeEnum.INVALID_TIME;
            }

            if (latestCreatedTime.HasValue && r.Date > latestCreatedTime.Value.AddDays(-1))
            {
                return GymUsageValidTypeEnum.IMPORT_NOT_YET;
            }

            if (!r.IsWorkDay() && !isMakeupDay)
            {
                return GymUsageValidTypeEnum.SKIP_NON_WORKDAY;
            }

            if (r.IsWorkDay() && isHoliday)
            {
                return GymUsageValidTypeEnum.SKIP_HOLIDAY;
            }

            if (isMakeupDay && !r.GymRecordTime.HasValue)
            {
                return GymUsageValidTypeEnum.NO_MAKEUP_DAY;
            }

            if (r.IsWorkDay() && !r.GymRecordTime.HasValue && !r.SpeedGateRecordTime.HasValue)
            {
                return GymUsageValidTypeEnum.NO_RECORD;
            }

            return null;
        }
    }

    //public class GymUsageCommonTests
    //{
    //    [Theory]
    //    public void CalcBurden_DifferentInputs_ShouldReturnCorrectValue()
    //    {

    //    }
    //}
}
