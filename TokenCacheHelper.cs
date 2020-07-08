using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Acn.Portal.Support.Common
{
    public class TokenCacheHelper : TokenCache
    {
        private HttpContextBase context;
        private static readonly object FileLock = new object();
        private readonly string CacheId;
        public string UserObjectId;

        public TokenCacheHelper(string userId, HttpContextBase context)
        {
            this.context = context;
            this.UserObjectId = userId;
            this.CacheId = UserObjectId + "_TokenCache";

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public void Load()
        {
            lock (FileLock)
            {
                Deserialize((byte[])context.Session[CacheId]);
            }
        }

        public void Persist()
        {
            lock (FileLock)
            {
                // Reflect changes in the persistent store.
                var bytes = Serialize();
                var x = System.Text.Encoding.UTF8.GetString(bytes);
                context.Session[CacheId] = Serialize();

                // After the write operation takes place, restore the HasStateChanged bit to false.
                HasStateChanged = false;
            }
        }

        // Empties the persistent store.
        public void Clear(string clientId)
        {
            context.Session.Remove(CacheId);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after ADAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (HasStateChanged)
            {
                Persist();
            }
        }
    }
}   