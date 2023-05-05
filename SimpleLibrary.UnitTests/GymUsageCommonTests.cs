using SimpleLibrary;

namespace SimpleLibrary.UnitTests
{
    public class GymUsageCommonTests
    {
        [Theory]
        [InlineData("0", 503)]
        [InlineData("1", 503)]
        [InlineData("43", 503)]
        [InlineData("44", 404)]
        [InlineData("45", 404)]
        [InlineData("58", 404)]
        [InlineData("59", 0)]
        [InlineData("60", 0)]
        [InlineData("61", 0)]
        public void CalcBurden_DifferentInputs_ShouldReturnCorrectValue(
            string input, int expected)
        {
            int result = GymUsageCommon.CalcBurden(decimal.Parse(input), out string _);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FillMissingDays_InitEmpty_FillAll()
        {
            // Arrange
            List<UserGymUsageDailyReportDTO> dailyReport = new();
            DateTime startDate = new DateTime(2021, 1, 1);
            DateTime endDate = new DateTime(2021, 1, 31);

            // Act
            GymUsageCommon.FillMissingDays(dailyReport,
                startDate, endDate);

            // Assert
            Assert.Equal(31, dailyReport.Count);
        }

        [Fact]
        public void ProcessReportDetailAndCalcTotalGymUsage_NoUsageWithPeriod_ZeroUsage()
        {
            // Arrange
            DateTime startDate = new DateTime(2021, 1, 1);
            DateTime endDate = new DateTime(2021, 1, 1);
            DateTime? latestCreatedTime = null;
            List<GymPermit> empGymPermits = new()
            {
                new GymPermit
                {
                    PermitId = Guid.NewGuid(),
                    ValidTime = new DateTime(2020, 1, 1),
                    InvalidTime = new DateTime(2021, 12, 31),
                }
            };
            List<UserGymUsageDailyReportDTO> dailyReport = new()
            {
                new UserGymUsageDailyReportDTO
                {
                    Date = new DateTime(2021, 1, 1),
                    GymIdNumber = empGymPermits[0].PermitId.ToString(),
                    SpeedGateRecordTime = new DateTime(2021, 1, 1, 8, 0, 0),
                }
            };
            List<HolidayMakeupDay> specialDateList = new();
            List<GymUsageReportDetailViewModel> detailList;

            // Act
            decimal result = GymUsageCommon.ProcessReportDetailAndCalcTotalGymUsage(
                startDate, endDate, latestCreatedTime,
                empGymPermits, dailyReport, specialDateList,
                out detailList);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessReportDetailAndCalcTotalGymUsage_UseEverydayWithPeriod_100Usage()
        {
            // Arrange
            DateTime startDate = new DateTime(2021, 1, 1);
            DateTime endDate = new DateTime(2021, 1, 2);
            DateTime? latestCreatedTime = null;
            List<GymPermit> empGymPermits = new()
            {
                new GymPermit
                {
                    PermitId = Guid.NewGuid(),
                    ValidTime = new DateTime(2020, 1, 1),
                    InvalidTime = new DateTime(2021, 12, 31),
                }
            };
            List<UserGymUsageDailyReportDTO> dailyReport = new()
            {
                new UserGymUsageDailyReportDTO
                {
                    Date = new DateTime(2021, 1, 1),
                    GymIdNumber = empGymPermits[0].PermitId.ToString(),
                    GymRecordTime = new DateTime(2021, 1, 1, 18, 0, 0),
                    SpeedGateRecordTime = new DateTime(2021, 1, 1, 8, 0, 0),
                },
                new UserGymUsageDailyReportDTO
                {
                    Date = new DateTime(2021, 1, 1),
                    GymIdNumber = empGymPermits[0].PermitId.ToString(),
                    GymRecordTime = new DateTime(2021, 1, 2, 18, 0, 0),
                    SpeedGateRecordTime = new DateTime(2021, 1, 1, 8, 0, 0),
                }
            };
            List<HolidayMakeupDay> specialDateList = new();
            List<GymUsageReportDetailViewModel> detailList;

            // Act
            decimal result = GymUsageCommon.ProcessReportDetailAndCalcTotalGymUsage(
                startDate, endDate, latestCreatedTime,
                empGymPermits, dailyReport, specialDateList,
                out detailList);

            // Assert
            Assert.Equal(100, result);
        }
    }
}