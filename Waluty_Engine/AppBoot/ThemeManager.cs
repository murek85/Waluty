using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace Waluty.Engine.AppBoot
{
    [Export(typeof(IThemeManager))]
    public class ThemeManager : IThemeManager
    {
        #region Private Variables

        private readonly ResourceDictionary themeResources;

        #endregion

        #region Constructor

        public ThemeManager()
        {
            this.themeResources = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Waluty.Engine;component/Resources/Theme.xaml")
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ResourceDictionary GetThemeResources()
        {
            return this.themeResources;
        }

        #endregion
    }
}
