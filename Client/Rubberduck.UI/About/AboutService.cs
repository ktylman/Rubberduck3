﻿using Microsoft.Extensions.Logging;
using Rubberduck.SettingsProvider;
using Rubberduck.UI.Services;

namespace Rubberduck.UI.About
{
    public class AboutService : WindowService<AboutWindow, IAboutWindowViewModel>
    {
        public AboutService(ILogger<WindowService<AboutWindow, IAboutWindowViewModel>> logger, RubberduckSettingsProvider settings, IAboutWindowViewModel viewModel, PerformanceRecordAggregator performance)
            : base(logger, settings, viewModel, performance)
        {
        }

        protected override AboutWindow CreateWindow(IAboutWindowViewModel model) => new(model);
    }
}
