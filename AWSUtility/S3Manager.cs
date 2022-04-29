using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace TypeKitProxyApp {
    public class S3Manager {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<S3Manager> _logger;

        private readonly string awsAccessKeyId = null;
        private readonly string awsSecretAccessKey = null;
        private readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
        private readonly string bucketName = null;

        private IAmazonS3 client;

        public S3Manager(ILoggerFactory loggerFactory, String strAWSAccessKeyId, String strAWSSecretAccessKey, String strAWSDefaultRegion, String strBucketName) {
            this._loggerFactory = loggerFactory;
            this._logger = this._loggerFactory.CreateLogger<S3Manager>();

            if (!String.IsNullOrEmpty(strAWSAccessKeyId)) {
                this.awsAccessKeyId = strAWSAccessKeyId;
            }

            if (!String.IsNullOrEmpty(strAWSSecretAccessKey)) {
                this.awsSecretAccessKey = strAWSSecretAccessKey;
            }

            if (!String.IsNullOrEmpty(strAWSDefaultRegion)) {
                this.bucketRegion = RegionEndpoint.GetBySystemName(strAWSDefaultRegion);
            }

            if (!String.IsNullOrEmpty(strBucketName)) {
                this.bucketName = strBucketName;
            }

            /*
            AmazonS3Config objConfig = new AmazonS3Config() {
                RegionEndpoint = this.bucketRegion,
                SignatureVersion = "v4"
            };
            */

            this.client = new AmazonS3Client(this.awsAccessKeyId, this.awsSecretAccessKey, this.bucketRegion);
        }

        public async Task<String> GetData(String strKey) {
            string strResponseBody = "";
            
            try {
                strKey = strKey.ToLower();  //We are saying all keys as in lowercase format...

                GetObjectRequest request = new GetObjectRequest {
                    BucketName = bucketName,
                    Key = strKey
                };

                using (GetObjectResponse response = await this.client.GetObjectAsync(request)) {
                    using (Stream responseStream = response.ResponseStream) {
                        using (GZipStream gzipStream = new GZipStream(responseStream, CompressionMode.Decompress, true)) {
                            using (StreamReader reader = new StreamReader(gzipStream)) {
                                strResponseBody = reader.ReadToEnd();
                            }
                        }
                    }
                }

                return strResponseBody;
            }
            catch (AmazonS3Exception exS3) {
                _logger.LogError(500, exS3.Message + "; Key: " + strKey);
                throw exS3;
            }
            catch (Exception ex) {
                _logger.LogError(500, ex.Message + "; Key: " + strKey);
                throw ex;
            }
        }

        public async Task<String> SetData(Stream sOriginal, String strKey, String strContentType = "") {
            String strVersionID = string.Empty;

            try {
                if (sOriginal.Length > 0 && !string.IsNullOrEmpty(strKey)) {
                    PutObjectRequest objRequest = new PutObjectRequest() {
                        AutoCloseStream = false,
                        BucketName = bucketName,
                        Key = strKey.ToLower(),
                        InputStream = sOriginal
                    };
                    objRequest.Headers.CacheControl = "public, max-age=31536000, s-maxage=31536000";   //Keep Object in AWS CloudFront Edge for a year as well as in Client Browser.
                    if (!String.IsNullOrEmpty(strContentType)) {
                        objRequest.Headers.ContentType = strContentType;
                    }
                    
                    objRequest.Metadata.Add("Lib", "TypeKitProxyApp 1.0.0.0");

                    PutObjectResponse objResponse = await this.client.PutObjectAsync(objRequest);

                    strVersionID = objResponse.VersionId;
                }
                else {
                    throw new ArgumentException("Could not read argument(s) sOriginal or strKey");
                }
            }
            catch (AmazonS3Exception ex) {
                if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") || ex.ErrorCode.Equals("InvalidSecurity"))) {
                    throw new Exception("Check the provided AWS Credentials. For service sign up go to http://aws.amazon.com/s3", ex);
                }
                else {
                    throw new Exception("Error occurred when uploading an object to S3 Bucket", ex);
                }
            }

            return strVersionID;
        }

        public async Task<String> SetCompressedData(Stream sOriginal, String strKey, String strContentType = "") {
            String strVersionID = string.Empty;

            try {
                if (sOriginal.Length > 0 && !string.IsNullOrEmpty(strKey)) {
                    byte[] bSource = sOriginal.ToByteArray();

                    using (MemoryStream mStream = new MemoryStream()) {
                        using (GZipStream gzipStream = new GZipStream(mStream, CompressionMode.Compress, true)) {
                            gzipStream.Write(bSource, 0, bSource.Length);   //Write from empty stream to bSource
                            gzipStream.Close();                             //Close GZip Stream so, all the bytes will be written to mStream object
                            mStream.Position = 0;                           //Move Stream at the begining position

                            PutObjectRequest objRequest = new PutObjectRequest() {
                                AutoCloseStream = false,
                                BucketName = bucketName,
                                Key = strKey.ToLower(),
                                InputStream = mStream
                            };

                            objRequest.Headers.CacheControl = "public, max-age=31536000, s-maxage=31536000";      //Keep Object in AWS CloudFront Edge for a year as well as in Client Browser.
                            objRequest.Headers.ContentEncoding = "gzip";                                        //Change encoding to gzip since we are converting to GZipStream
                            if (!String.IsNullOrEmpty(strContentType)) {
                                objRequest.Headers.ContentType = strContentType;
                            }
                            objRequest.Metadata.Add("Lib", "TypeKitProxyApp 1.0.0.0");

                            PutObjectResponse objResponse = await client.PutObjectAsync(objRequest);

                            strVersionID = objResponse.VersionId;
                        }
                    }
                }
                else {
                    throw new ArgumentException("Could not read argument(s) sOriginal or strKey");
                }
            }
            catch (AmazonS3Exception ex) {
                if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") || ex.ErrorCode.Equals("InvalidSecurity"))) {
                    throw new Exception("Check the provided AWS Credentials. For service sign up go to http://aws.amazon.com/s3", ex);
                }
                else {
                    throw new Exception("Error occurred when uploading an object to S3 Bucket", ex);
                }
            }

            return strVersionID;
        }
    }
}