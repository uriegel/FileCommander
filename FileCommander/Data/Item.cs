namespace FileCommander.Data;

record Item(int Index, string Text, string Icon, string[] Values, bool Hidden = false);

record ItemResult(Item[] Items, int Pos);

static class ItemExtensions
{
    public static string FormatSize(this long size)
    {
        if (size == -1)
            return "";
        var sizeStr = size.ToString();
        var sep = '.';
        if (sizeStr.Length > 3)
        {
            var sizePart = sizeStr;
            sizeStr = "";
            for (var j = 3; j < sizePart.Length; j += 3)
            {
                var extract = sizePart.Substring(sizePart.Length - j, 3);
                sizeStr = sep + extract + sizeStr;
            }
            var strfirst = sizePart[..((sizePart.Length % 3 == 0) ? 3 : (sizePart.Length % 3))];
            sizeStr = strfirst + sizeStr;
        }
        return sizeStr;
    }
}
