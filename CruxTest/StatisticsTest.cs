using Crux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CruxTest
{
    [TestClass]
    public class StatisticsTest
    {
        [TestMethod]
        public void SerializationTest()
        {
            Statistics stats = new Statistics();
            stats.Snapshot(1, 1, 15);
            stats.Snapshot(1.1, 1.1, 15.2);
            stats.Snapshot(1.2, 1.4, 14.75);
            stats.Snapshot(10, 1, 15.5);

            for (int i = 0; i < stats.Snapshots.Count; i++)
            {
                stats.Snapshots[i].Time = DateTime.Now.AddHours(i);
            }

            var sRatioBefore = stats.SharpeRatio();

            string filename = "logtest.txt";
            stats.Export(filename);

            Statistics stats2 = new Statistics();
            stats2.Import(filename);
            var sRatioAfter = stats2.SharpeRatio();

            Assert.AreEqual(sRatioBefore, sRatioAfter);
        }
    }
}
