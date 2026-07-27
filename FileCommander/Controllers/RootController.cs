namespace FileCommander.Controllers;

class RootController : Controller
{
    public const string Name = "root";

    public static RootController Get(Controller? current)
        => current is RootController rootController
            ? rootController
            : new RootController(current);

    public RootController(Controller? previous)
    {

    }
}
