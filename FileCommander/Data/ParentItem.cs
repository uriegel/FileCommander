namespace FileCommander.Data;

public record ParentItem : Item
{
    public ParentItem() => Name = "..";
}
