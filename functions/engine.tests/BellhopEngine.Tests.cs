using System;
using System.Collections;
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


        [Fact]
        public void TestResizeTime_StaticInputs_True()
        {
            //Friday April 2 2021 at 10:00
            DateTime now = new DateTime(2021, 4, 2, 10, 0, 0);
            Hashtable times = new Hashtable() {
                {"StartTime", "Friday 9AM"},
                {"EndTime", "Friday 5PM"}
            };

            bool result = BellhopEngine.resizeTime(times, now);
            Assert.True(result, "Result expected true for static inputs inside window");
        }

        [Fact]
        public void TestResizeTime_StaticInputs_False()
        {
            //Friday April 2 2021 at 10:00
            DateTime now = new DateTime(2021, 4, 2, 10, 0, 0);
            Hashtable times = new Hashtable() {
                {"StartTime", "Friday 8AM"},
                {"EndTime", "Friday 9AM"}
            };

            bool result = BellhopEngine.resizeTime(times, now);
            Assert.False(result, "Result expected false for static inputs outside window");
        }

        [Fact]
        public void TestResizeTime_StaticInputs2_True()
        {
            //Friday March 19 2021 at 10:00
            DateTime now = new DateTime(2021, 3, 19, 10, 0, 0);
            Hashtable times = new Hashtable() {
                {"StartTime", "Friday 9AM"},
                {"EndTime", "Friday 5PM"}
            };

            bool result = BellhopEngine.resizeTime(times, now);
            Assert.True(result, "Result expected true for static inputs using a predefined 'now' and inputs inside window");
        }

        [Fact]
        public void TestResizeTime_StaticInputs2_False()
        {
            //Friday March 19 2021 at 10:00
            DateTime now = new DateTime(2021, 3, 19, 10, 0, 0);
            Hashtable times = new Hashtable() {
                {"StartTime", "Friday 8AM"},
                {"EndTime", "Friday 9AM"}
            };

            bool result = BellhopEngine.resizeTime(times, now);
            Assert.False(result, "Result expected false for static inputs using a predefined 'now' and inputs outside window");
        }

    }
}
