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
            
            String strKey = "Method=GetTypeKitAsync&strTypeKitCode=" + strTypeKitCode;

            // Step 0: Find in Local Memory!
            strCode = (String)this._inMemoryCache.Get(strKey);

            if (String.IsNullOrEmpty(strCode)) {
                // Step 1: Find in Memcache
                strCode = (String)this._memcacheService.Get(strKey);

                if (String.IsNullOrEmpty(strCode)) {
                    // Step 2: Find in S3 Bucket
                    strCode = await _s3Service.GetObjectAsync(strTypeKitCode);

                    if (String.IsNullOrEmpty(strCode)) {
                        // Step 3: Fetch from Adobe TypeKit's Server
                        strCode = await this.GetTypeKitProjectAsync(new Uri("https://use.typekit.com/" + strTypeKitCode));

                        if (!String.IsNullOrEmpty(strCode)) {
                            // Step 3.1: Sanitize and Compress
                            strCode = this.getCompressedJS(this.sanitizeJS(strCode));

                            if (!String.IsNullOrEmpty(strCode)) {
                                // Step 4: Save to S3 Bucket
                                Boolean blnS3SaveResult = await _s3Service.SetObjectAsync(strTypeKitCode, strCode, "text/javascript");

                                // Step 5: Save to Memcache   
                                Boolean blnMemcacheSaveResult = await this._memcacheService.SetAsync(strKey, strCode, strGroupKey);

                                // Step 6: Save to Local Memory
                                this._inMemoryCache.Set(strKey, strCode);
                            }
                        }
                    }
                    else {
                        // Step 5: Save to Memcache
                        Boolean blnMemcacheSaveResult = await this._memcacheService.SetAsync(strKey, strCode, strGroupKey);

                        // Step 6: Save to Local Memory
                        this._inMemoryCache.Set(strKey, strCode);
                    }
                }
            }

            return strCode;
        }

        public async Task<String> GetTypeKitCSSAsync(String strTypeKitCode) {
            String strCode = String.Empty;
            
            String strKey = "Method=GetTypeKitAsync&strTypeKitCode=" + strTypeKitCode;

            // Step 0: Find in Local Memory!
            strCode = (String)this._inMemoryCache.Get(strKey);

            if (String.IsNullOrEmpty(strCode)) {
                // Step 1: Find in Memcache
                strCode = (String)this._memcacheService.Get(strKey);

                if (String.IsNullOrEmpty(strCode)) {
                    // Step 2: Find in S3 Bucket
                    strCode = await _s3Service.GetObjectAsync(strTypeKitCode);

                    if (String.IsNullOrEmpty(strCode)) {
                        // Step 3: Fetch from Adobe TypeKit's Server
                        strCode = await this.GetTypeKitProjectAsync(new Uri("https://use.typekit.com/" + strTypeKitCode));

                        if (!String.IsNullOrEmpty(strCode)) {
                            // Step 3.1: Sanitize and Compress
                            strCode = this.getCompressedCSS(this.sanitizeCSS(strCode));

                            if (!String.IsNullOrEmpty(strCode)) {
                                // Step 4: Save to S3 Bucket
                                Boolean blnS3SaveResult = await _s3Service.SetObjectAsync(strTypeKitCode, strCode, "text/css");

                                // Step 5: Save to Memcache   
                                Boolean blnMemcacheSaveResult = await this._memcacheService.SetAsync(strKey, strCode, strGroupKey);

                                // Step 6: Save to Local Memory
                                this._inMemoryCache.Set(strKey, strCode);
                            }
                        }
                    }
                    else {
                        // Step 5: Save to Memcache
                        Boolean blnMemcacheSaveResult = await this._memcacheService.SetAsync(strKey, strCode, strGroupKey);

                        // Step 6: Save to Local Memory
                        this._inMemoryCache.Set(strKey, strCode);   
                    }
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