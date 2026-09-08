using System.Web.Mvc;
using System.Web.Routing;

namespace GamaspEngenharia
{
    public class RouteConfig
    {
        /// <summary>
        /// Namespace onde vivem os controllers deste site.
        ///
        /// Fixar isso evita o erro "Multiple types were found that match the
        /// controller named 'Home'": o MVC varre todas as DLLs do bin, então
        /// uma sobra de deploy antigo (por exemplo CaioUechi.dll, de quando o
        /// projeto tinha outro nome) passaria a concorrer pelas rotas e
        /// derrubaria o site inteiro.
        /// </summary>
        private static readonly string[] Namespaces = { "GamaspEngenharia.Controllers" };

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            Fixar(routes.MapRoute(
                name: "RegularizacaoRedirect",
                url: "regularizacao-de-imoveis-com-parceria-tecnica-especializada",
                defaults: new { controller = "Home", action = "RegularizacaoImovel" },
                constraints: null,
                namespaces: Namespaces
            ));

            Fixar(routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                constraints: null,
                namespaces: Namespaces
            ));
        }

        /// <summary>
        /// Desliga a busca em outros namespaces. Sem isto o MVC ainda voltaria
        /// a varrer o bin inteiro caso não achasse o controller no nosso.
        ///
        /// Só se aplica às rotas criadas aqui: a rota do IgnoreRoute nasce com
        /// DataTokens nulo e quebraria com NullReferenceException.
        /// </summary>
        private static void Fixar(Route rota)
        {
            if (rota == null) return;

            if (rota.DataTokens == null)
            {
                rota.DataTokens = new RouteValueDictionary();
            }

            rota.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
