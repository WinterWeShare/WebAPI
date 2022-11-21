using System.Diagnostics;
using WebAPI;
using WebAPI.Controllers;

namespace Tests
{
	[TestClass]
	public class UnitTest1
	{
		Controller controller = new();
		
		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			Program.Main(Array.Empty<string>());
		}


		[TestMethod]
		public void TestMethod1()
		{
			controller.GetUserEmails().ToList().ForEach(s => Debug.Print(s));
		}
	}
}