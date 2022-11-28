using System.Diagnostics;
using WebAPI;
using WebAPI.Controllers;

namespace Tests
{
	[TestClass]
	public class UnitTest
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
			controller.IsUserExist().ToList().ForEach(s => Debug.Print(s));
		}
	}
}