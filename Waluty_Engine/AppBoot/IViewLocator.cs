using System;
using System.Windows;

namespace Waluty.Engine.AppBoot
{
    public interface IViewLocator
    {
        UIElement GetOrCreateViewType(Type viewType);
    }
}
