using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace FileCommander.Controls;

public class SplitterGrid : Grid
{
    public SplitterGrid()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
