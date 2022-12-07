using WebAPI;
using WebAPI.Controllers;

namespace Tests;

[TestClass]
public class UnitTest
{
    private ClientController _clientController = new();

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Program.Main(Array.Empty<string>());
    }


    [TestMethod]
    public void TestMethod1()
    {
    }
}