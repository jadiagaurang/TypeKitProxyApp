using System;
using System.Threading.Tasks;

using Enyim.Caching;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Logging;

namespace TypeKitProxyApp {
    public class MemcacheManager : IMemcacheService {
        private readonly ILogger<MemcacheManager> _logger;
        private readonly IMemcachedClient _enyimCachedClient;

        public MemcacheManager(ILogger<MemcacheManager> logger, IMemcachedClient enyimCachedClient) {        
            this._logger = logger;
            this._enyimCachedClient = enyimCachedClient;
        }

        public object Get(String key) {
            object objValue = null;
            
            try {
                key = key.Replace(" ", "_");
                objValue = _enyimCachedClient.Get(key);
            }
            catch (Exception ex) {
                _logger.LogError(500, String.Format("MemcacheManager.Get exception message = {0}", ex.Message), ex);
                objValue = null;
            }           

            return objValue;
        }

        public async Task<Boolean> SetAsync(String key, Object objValue, String groupKey) {
            return await this.SetAsync(key, objValue, groupKey, 10080);
        }

        public async Task<Boolean> SetAsync(String key, Object objValue, String groupKey, Int32 validMinutes = 10080) {
            //Argument Check
            if (string.IsNullOrEmpty(key)) {
                _logger.LogError(500, String.Format("MemcacheManager.SetAsync missing parameters: key = {0}", key));
                return false;                
            }

            if (string.IsNullOrEmpty(groupKey)) {
                _logger.LogError(500, String.Format("MemcacheManager.SetAsync missing parameters: groupKey = {0}", groupKey));
                return false;                
            }

            //Memcache can't save null objects
            if (objValue == null) {
                _logger.LogError(500, String.Format("MemcacheManager.SetAsync missing parameters: objValue = {0}", objValue));
                return false;
            }            

            try {
                key = key.Replace(" ", "_");
                groupKey = groupKey.Replace(" ", "_");

                //Store Key/Value Here
                if (_enyimCachedClient.Get<object>(key) == null) {                   
                    return await _enyimCachedClient.StoreAsync(StoreMode.Set, key, objValue, new TimeSpan(0, validMinutes, 0));                   
                }
                else {
                    return await _enyimCachedClient.StoreAsync(StoreMode.Replace, key, objValue, new TimeSpan(0, validMinutes, 0));                 
                }                
            }
            catch (Exception ex) {
                _logger.LogError(500, String.Format("MemcacheManager.SetAsync exception message = {0}", ex.Message), ex);
                return false;
            }
        }
    } 
}