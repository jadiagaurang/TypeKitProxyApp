using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TypeKitProxyApp {
    public class TypeKitController : Controller {
        private readonly ILogger<TypeKitController> _logger;
        private readonly IConfiguration _config;
        private readonly ITypeKitService _typeKitService;

        public TypeKitController(ILogger<TypeKitController> logger, IConfiguration configuration, ITypeKitService typeKitService) {
            this._logger = logger;
            this._config = configuration;
            this._typeKitService = typeKitService;
        }

        [HttpGet("/{TypeKitCode}")]
        public async Task<IActionResult> Index(String TypeKitCode) {
            try {
                if (!String.IsNullOrEmpty(TypeKitCode) && TypeKitCode.EndsWith(".js")) {
                    String strCode = await this._typeKitService.GetTypeKitJSAsync(TypeKitCode);

                    return Content(strCode, "text/javascript");
                }
                else if (!String.IsNullOrEmpty(TypeKitCode) && TypeKitCode.EndsWith(".css")) {
                    String strCode = await this._typeKitService.GetTypeKitCSSAsync(TypeKitCode);

                    return Content(strCode, "text/css");
                }
            }
            catch (Exception ex) {
                this._logger.LogError(500, ex.Message, ex);
            }
            
            return CatchAll();
        }

        [Route("/{**catchAll}")]
        public IActionResult CatchAll() {
            return NotFound("not found");    //("not found", "text/plain;charset=utf-8");
        }
    }
}
