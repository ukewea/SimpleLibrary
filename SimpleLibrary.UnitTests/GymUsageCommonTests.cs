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
        public void FillMissingDays_Test()
        {
            // Arrange
            List<UserGymUsageDailyReportDTO> dailyReport = new();
            DateTime startDate = new DateTime(2021, 1, 1);
            DateTime endDate = new DateTime(2021, 1, 31);

            // Act
            GymUsageCommon.FillMissingDays(dailyReport,
                startDate, endDate);

            // Assert
            Assert.Equal(dailyReport.Count, 31);
        }

        [Fact]
        public void ProcessReportDetailAndCalcTotalGymUsage_Test()
        {

        }

    }
}