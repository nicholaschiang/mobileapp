using System;
using Toggl.Core.Services;
using Foundation;
using Toggl.iOS.ExtensionKit;

namespace Toggl.iOS.Services
{
    public class PrivateSharedStorageServiceIos : IPrivateSharedStorageService
    {
        public void SaveApiToken(string apiToken)
        {
            SharedStorage.instance.SetApiToken(apiToken);
        }

        public void SaveUserId(long userId)
        {
            SharedStorage.instance.SetUserId(userId);
        }

        public void SaveDefaultWorkspaceId(long workspaceId)
        {
            SharedStorage.instance.SetDefaultWorkspaceId(workspaceId);
        }

        public void ClearAll()
        {
            SharedStorage.instance.DeleteEverything();
        }

        public bool HasUserDataStored()
            => !string.IsNullOrEmpty(SharedStorage.instance.GetApiToken());
        
        public string GetApiToken()
            => SharedStorage.instance.GetApiToken();
    }
}
