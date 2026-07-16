using FileCommander.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Text;

using Windows.Networking.Connectivity;
using Windows.Storage;

namespace FileCommander.DataTemplates;

public class TemplateSelector : IElementFactory
{
    public DataTemplate? Normal { get; set; }

    public UIElement GetElement(ElementFactoryGetArgs args)
    {
        ItemGrid row;
        if (pool.Count > 0)
        {
            row = (ItemGrid)pool.Pop();
        }
        else
        {
            row = (ItemGrid)Normal!.LoadContent();
            row.TypeName = "Uwe Riegel";
        }
        return row;
    }

    void IElementFactory.RecycleElement(ElementFactoryRecycleArgs args) 
    {
        if (args.Element is ItemGrid row)
        {
            pool.Push(row);
        }
    }

    readonly Stack<UIElement> pool = new();
}

