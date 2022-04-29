using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TypeKitProxyApp {
    public class S3Service : IS3Service {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<S3Service> _logger;

        private readonly S3Manager objClient = null;

        public S3Service(ILoggerFactory loggerFactory, ILogger<S3Service> logger, IConfiguration configuration) {
            this._loggerFactory = loggerFactory;
            this._logger = logger;

            this.objClient = new S3Manager(
                this._loggerFactory,
                configuration.GetSection("AWS:AWS_ACCESS_KEY_ID").Value, 
                configuration.GetSection("AWS:AWS_SECRET_ACCESS_KEY").Value,
                configuration.GetSection("AWS:AWS_DEFAULT_REGION").Value,
                configuration.GetSection("AWS:AWS_S3_BUCKET").Value
            );
        }

        public async Task<String> GetObjectAsync(String strKey) {
            try {
                return await this.objClient.GetData(strKey);
            }
            catch (Exception ex) {
                this._logger.LogError(500, ex.Message, ex);

                return null;
            }
        }

        public async Task<Boolean> SetObjectAsync(String strKey, String strContent, String strContentType = "") {
            try {
                using (MemoryStream mStream = new MemoryStream(Encoding.UTF8.GetBytes(strContent))) {
                    String strVersionId = await this.objClient.SetCompressedData(mStream, strKey, strContentType);

                    return true;
                }
            }
            catch (Exception ex) {
                this._logger.LogError(500, ex.Message, ex);

                return false;
            }
        }
    }
}