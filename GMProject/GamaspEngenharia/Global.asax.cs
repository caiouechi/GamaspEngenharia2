using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace GamaspEngenharia
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// Redireciona HTTP para HTTPS quando a chave ForcarHttps estiver
        /// ligada no Web.config.
        ///
        /// Vem desligada de propósito: se a hospedagem terminar o TLS num
        /// proxy e repassar a requisição em HTTP puro, um redirecionamento
        /// ingênuo cria laço infinito e derruba o site. Por isso só ligue
        /// depois de conferir que o certificado responde e que o cabeçalho
        /// X-Forwarded-Proto (tratado abaixo) chega corretamente.
        /// </summary>
        protected void Application_BeginRequest()
        {
            if (!ForcarHttps) return;

            var pedido = Request;

            // Já está seguro.
            if (pedido.IsSecureConnection) return;

            // Proxy/balanceador que terminou o TLS antes de chegar aqui.
            var repassado = pedido.Headers["X-Forwarded-Proto"];
            if (!string.IsNullOrEmpty(repassado) &&
                repassado.IndexOf("https", StringComparison.OrdinalIgnoreCase) >= 0) return;

            // Em desenvolvimento não atrapalha.
            if (pedido.IsLocal) return;

            // A validação do Let's Encrypt precisa funcionar em HTTP puro:
            // redirecionar o desafio quebraria a renovação do certificado.
            var caminho = pedido.Url.AbsolutePath;
            if (caminho.StartsWith("/.well-known/", StringComparison.OrdinalIgnoreCase)) return;

            // Redirecionar POST descartaria o corpo da requisição.
            if (!pedido.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                !pedido.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase)) return;

            var destino = new UriBuilder(pedido.Url) { Scheme = Uri.UriSchemeHttps, Port = -1 };
            Response.RedirectPermanent(destino.Uri.AbsoluteUri, endResponse: true);
        }

        private static bool ForcarHttps
        {
            get
            {
                bool ligado;
                var valor = ConfigurationManager.AppSettings["ForcarHttps"];
                return bool.TryParse(valor, out ligado) && ligado;
            }
        }
    }
}
