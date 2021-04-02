using System;
using Xunit;

using Bellhop.Function;

namespace engine.tests
{
    public class BellhopEngineTests
    {
        [Fact]
        public void TestGetActionTime_Tuesday9AM()
        {
            (System.DayOfWeek day,  TimeSpan time) = BellhopEngine.getActionTime("Tuesday 9AM");
            Assert.True(day.Equals(DayOfWeek.Tuesday), "Day of week should be Tuesday");
            Assert.True(time.Equals(new TimeSpan(9,0,0)), "Hour should be 9AM (09:00)");
        }

        [Fact]
        public void TestGetActionTime_Friday1PM()
        {
            (System.DayOfWeek day,  TimeSpan time) = BellhopEngine.getActionTime("Friday 1PM");
            Assert.True(day.Equals(DayOfWeek.Friday), "Day of week should be Friday");
            Assert.True(time.Equals(new TimeSpan(13,0,0)), "Hour should be 1PM (13:00)");
        }

        [Fact]
        public void TestGetActionTime_Friday1300()
        {
            (System.DayOfWeek day,  TimeSpan time) = BellhopEngine.getActionTime("Friday 13:00");
            Assert.True(day.Equals(DayOfWeek.Friday), "Day of week should be Friday");
            Assert.True(time.Equals(new TimeSpan(13,0,0)), "Hour should be 1PM (13:00)");
        }

        [Fact]
        public void TestGetActionTime_Daily1300PM()
        {
            (System.DayOfWeek day,  TimeSpan time) = BellhopEngine.getActionTime("Daily 13:00");
            Assert.True(day.Equals(DateTime.Now.DayOfWeek), $"Day of week should be {DateTime.Now.DayOfWeek}");
            Assert.True(time.Equals(new TimeSpan(13,0,0)), "Hour should be 1PM (13:00)");
        }
    }
}
