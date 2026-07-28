using FileCommander.Data;

namespace FileCommander.Controllers;

class DirectoryController : Controller
{
    public DirectoryController(string path)
    {
        this.path = path;
    }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Datum"),
            new("Größe"),
            new("Version")
        ];

    public override Item[] GetItems()
    {
        return[];
    }

    public override OnProcessResult OnProcess(int pos)
    {
        return new();
    }

    string path;
}
