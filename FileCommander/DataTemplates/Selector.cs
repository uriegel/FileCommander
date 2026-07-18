using FileCommander.Controls;

using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileCommander.DataTemplates;

public class TemplateSelector : IElementFactory
{
    public ObservableCollection<NamedTemplate> Templates { get; } = [];

    public UIElement GetElement(ElementFactoryGetArgs args)
    {
        var type = args.Data.GetType();
        ItemGrid row;
        var pool = pools.TryGetValue(type, out var p) 
            ? p 
            : (pools[type] = new Stack<ItemGrid>());
        if (pool.Count > 0)
            row = pool.Pop();
        else
            row = (ItemGrid)lookup[type.Name].LoadContent();
        row.Type = type;
        return row;
    }

    public TemplateSelector()
    {
        Templates.CollectionChanged += (_, __) => RebuildLookup();
    }

    void IElementFactory.RecycleElement(ElementFactoryRecycleArgs args) 
    {
        if (args.Element is ItemGrid row && pools.TryGetValue(row.Type, out var pool))
        {
            row.Reset();
            pool.Push(row);
        }
    }

    void RebuildLookup()
    {
        lookup.Clear();
        foreach (var t in Templates)
            if (t.ItemDataTemplate != null)
                lookup[t.ItemName] = t.ItemDataTemplate;
    }

    readonly Dictionary<string, DataTemplate> lookup = [];
    readonly Dictionary<Type, Stack<ItemGrid>> pools = [];
}

public class NamedTemplate 
{
    public DataTemplate? ItemDataTemplate { get; set; }
    public NamedTemplate() { }
    public string ItemName { get; set; } = string.Empty;
}