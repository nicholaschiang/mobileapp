﻿using System;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Services;
using Toggl.Core.Services;
using Toggl.Networking;
using Toggl.Networking.Network;

namespace Toggl.Core.UI
{
    public abstract class UIDependencyContainer : DependencyContainer
    {
        private readonly Lazy<IUrlHandler> urlHandler;
        private readonly Lazy<IBrowserService> browserService;
        private readonly Lazy<ViewModelLoader> viewModelLoader;
        private readonly Lazy<INavigationService> navigationService;
        private readonly Lazy<IPermissionsChecker> permissionsService;

        public IUrlHandler UrlHandler => urlHandler.Value;
        public IBrowserService BrowserService => browserService.Value;
        public ViewModelLoader ViewModelLoader => viewModelLoader.Value;
        public INavigationService NavigationService => navigationService.Value;
        public IPermissionsChecker PermissionsChecker => permissionsService.Value;

        public static UIDependencyContainer Instance { get; protected set; }

        protected UIDependencyContainer(ApiEnvironment apiEnvironment, UserAgent userAgent)
            : base(apiEnvironment, userAgent)
        {
            urlHandler = new Lazy<IUrlHandler>(CreateUrlHandler);
            browserService = new Lazy<IBrowserService>(CreateBrowserService);
            viewModelLoader = new Lazy<ViewModelLoader>(CreateViewModelLoader);
            navigationService = new Lazy<INavigationService>(CreateNavigationService);
            permissionsService = new Lazy<IPermissionsChecker>(CreatePermissionsChecker);
        }

        protected abstract IUrlHandler CreateUrlHandler();
        protected abstract IBrowserService CreateBrowserService();
        protected abstract INavigationService CreateNavigationService();
        protected abstract IPermissionsChecker CreatePermissionsChecker();

        protected virtual ViewModelLoader CreateViewModelLoader() => new ViewModelLoader(this);
        protected override IErrorHandlingService CreateErrorHandlingService()
            => new ErrorHandlingService(NavigationService, AccessRestrictionStorage);
    }
}
