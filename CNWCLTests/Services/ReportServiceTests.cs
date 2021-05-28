using System.Threading.Tasks;
using CNWCL.Models;
using CNWCL.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CNWCLTests.Services
{
    [TestClass]
    public class ReportServiceTests
    {
        [TestMethod]
        public async Task GetCastListTest()
        {
            var report = await ReportService.GetReportByReportId("RxPbKg8cwQMfptdV");
            var dic= await ReportService.GetCastAsync(report, 1, 1, false);
            Assert.IsTrue(dic.Count>0);
        }
    }
}