using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Yahoo.Yui.Compressor;

namespace TypeKitProxyApp {
    public class TypeKitService : ITypeKitService {
        private readonly ILogger<TypeKitService> _logger;
        private readonly HttpClient _httpClient = null;
        private readonly String strAdobeTypeKitHost = "https://use.typekit.com/";
        private readonly String strGroupKey = "TypeKitProxyApp";

        private readonly IConfiguration _config;
        private readonly IMemoryCache _inMemoryCache;
        private readonly IMemcacheService _memcacheService;
        private readonly IS3Service _s3Service;
        private readonly String _envname = String.Empty;

        public TypeKitService(ILogger<TypeKitService> logger, IConfiguration configuration, IMemoryCache inMemoryCache, 
            IMemcacheService memcacheService, IS3Service s3Service, IWebHostEnvironment env) {

            this._logger = logger;

            this._config = configuration;

            this._inMemoryCache = inMemoryCache;
            this._memcacheService = memcacheService;

            this._s3Service = s3Service;

            this._envname = env.EnvironmentName;

            this._httpClient = new HttpClient(new HttpClientHandler() {
                UseCookies = false,
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }, //ignore ssl certificates.
                PreAuthenticate = false,
                CheckCertificateRevocationList = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                UseDefaultCredentials = false,
                UseProxy = false
            }, false) {
                Timeout = TimeSpan.FromSeconds(5)   // Timeout for 5 seconds only!
            };
            this._httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
        }

        public async Task<String> GetTypeKitJSAsync(String strTypeKitCode) {
            String strCode = String.Empty;
            
            String strKey = strTypeKitCode;

            // Step 1: Find in Cache!
            strCode = await this.GetCacheValue(strKey);

            if (String.IsNullOrEmpty(strCode)) {
                // Step 2: Fetch from Adobe TypeKit's Server
                strCode = await this.GetTypeKitProjectAsync(new Uri(this.strAdobeTypeKitHost + strTypeKitCode));

                if (!String.IsNullOrEmpty(strCode)) {
                    // Step 3: Sanitize and Compress
                    strCode = this.getCompressedJS(this.sanitizeJS(strCode));

                    // Step 4: Save in Cache!
                    Boolean blnCacheSaveResult = await this.SetCacheValue(strKey, strCode);
                }
            }

            return strCode;
        }

        public async Task<String> GetTypeKitCSSAsync(String strTypeKitCode) {
            String strCode = String.Empty;
            
            String strKey = strTypeKitCode;

            // Step 1: Find in Local Memory!
            strCode = await this.GetCacheValue(strKey);

            if (String.IsNullOrEmpty(strCode)) {
                // Step 2: Fetch from Adobe TypeKit's Server
                strCode = await this.GetTypeKitProjectAsync(new Uri(this.strAdobeTypeKitHost + strTypeKitCode));

                if (!String.IsNullOrEmpty(strCode)) {
                    // Step 3: Sanitize and Compress
                    strCode = this.getCompressedCSS(this.sanitizeCSS(strCode));

                    // Step 4: Save in Cache!
                    Boolean blnCacheSaveResult = await this.SetCacheValue(strKey, strCode);
                }
            }

            return strCode;
        }
        
        private async Task<String> GetTypeKitProjectAsync(Uri uriFile) {
            try {
                String strFileName = Path.GetFileName(String.Format("{0}{1}{2}{3}", uriFile.Scheme, Uri.SchemeDelimiter, uriFile.Authority, uriFile.AbsolutePath));
                String strContentType = String.Empty;

                using (HttpResponseMessage objHttpResponse = this._httpClient.GetAsync(uriFile).GetAwaiter().GetResult()) {
                    if (objHttpResponse.StatusCode == HttpStatusCode.OK && objHttpResponse.Content != null) {
                        if (!String.IsNullOrEmpty(objHttpResponse.Content.Headers.ContentType.MediaType) && objHttpResponse.Content.Headers.ContentType.MediaType.ToLower().Split('/')[0] == "text") {
                            return await objHttpResponse.Content.ReadAsStringAsync();
                        }
                        else {
                            throw new WebException("Could not find supported ContentType from the given URL");
                        }
                    }
                    else {
                        throw new WebException("Could not reach the given URL");
                    }
                }
            }
            catch {
                throw new WebException("Could not reach the given URL");
            }
        }

        private async Task<String> GetCacheValue (String strKey) {
            String strCode = String.Empty;

            if (!String.IsNullOrEmpty(strKey)) {
                String strCacheEngine = this.getCacheEngine();
                
                if (!String.IsNullOrEmpty(strCacheEngine)) {
                    if (strCacheEngine.Equals("inmemory", StringComparison.InvariantCultureIgnoreCase)) {
                        strCode = (String)this._inMemoryCache.Get(strKey);
                    }
                    else if (strCacheEngine.Equals("memcache", StringComparison.InvariantCultureIgnoreCase)) {
                        strCode = (String)this._memcacheService.Get(strKey);
                    }
                    else if (strCacheEngine.Equals("awss3", StringComparison.InvariantCultureIgnoreCase)) {
                        strCode = await _s3Service.GetObjectAsync(strKey);
                    }
                }
            }

            return strCode;
        }

        private async Task<Boolean> SetCacheValue (String strKey, String strValue) {
            Boolean blnResult = false;

            if (!String.IsNullOrEmpty(strKey) && !String.IsNullOrEmpty(strValue)) {
                String strCacheEngine = this.getCacheEngine();
                
                if (!String.IsNullOrEmpty(strCacheEngine)) {
                    if (strCacheEngine.Equals("inmemory", StringComparison.InvariantCultureIgnoreCase)) {
                        String strReturnValue = this._inMemoryCache.Set(strKey, strValue);

                        if (!String.IsNullOrEmpty(strReturnValue)) {
                            blnResult = true;
                        }
                    }
                    else if (strCacheEngine.Equals("memcache", StringComparison.InvariantCultureIgnoreCase)) {
                        blnResult = await this._memcacheService.SetAsync(strKey, strValue, strGroupKey);
                    }
                    else if (strCacheEngine.Equals("awss3", StringComparison.InvariantCultureIgnoreCase)) {
                        blnResult = await _s3Service.SetObjectAsync(strKey, strValue, "text/javascript");
                    }
                }
            }

            return blnResult;
        }

        private String getCacheEngine () {
            String strCacheEngine = "inmemory";    //Default Value

            if (this._config != null && !String.IsNullOrEmpty(this._config.GetSection("TypeKitProxyApp:CacheEngine").Value)) {
                switch (this._config.GetSection("TypeKitProxyApp:CacheEngine").Value) {
                    case "local":
                        strCacheEngine = "inmemory";
                        break;
                    case "memcache":
                        strCacheEngine = "memcache";
                        break;
                    case "s3":
                        strCacheEngine = "awss3";
                        break;
                }
            }

            return strCacheEngine;
        }

        private String sanitizeJS(String strJS) {
            return strJS.Replace("\"display\":\"auto\"", "\"display\":\"swap\"");
        }

        private String sanitizeCSS(String strCSS) {
            return strCSS.Replace("font-display:auto;", "font-display:swap;");
        }

        private String getCompressedJS(String strJS) {
            String strCompressedJS = String.Empty;

            if (!String.IsNullOrEmpty(strJS)) {
                try {
                    JavaScriptCompressor objJSC = new JavaScriptCompressor();
                    objJSC.CompressionType = CompressionType.Standard;
                    objJSC.ObfuscateJavascript = true;
                    objJSC.PreserveAllSemicolons = false;
                    objJSC.DisableOptimizations = false;
                    objJSC.LineBreakPosition = -1;
                    objJSC.LoggingType = LoggingType.None;
                    objJSC.ThreadCulture = CultureInfo.CreateSpecificCulture("en-US");
                    objJSC.IgnoreEval = false;

                    strCompressedJS = objJSC.Compress(strJS);
                }
                catch (Exception ex) {
                    this._logger.LogError(500, ex.Message, ex);

                    strCompressedJS = strJS;  //Error to return as it is
                }
            }

            return strCompressedJS;
        }

        private String getCompressedCSS(String strCSS) {
            String strCompressedCSS = string.Empty;

            if (!String.IsNullOrEmpty(strCSS)) {
                try {
                    CssCompressor objCC = new CssCompressor();
                    objCC.CompressionType = CompressionType.Standard;
                    objCC.LineBreakPosition = -1;
                    objCC.RemoveComments = true;

                    strCompressedCSS = objCC.Compress(strCSS);
                }
                catch (Exception ex) {
                    this._logger.LogError(500, ex.Message, ex);

                    strCompressedCSS = strCSS;  //Error to return as it is
                }
            }

            return strCompressedCSS;
        }
    }
}