using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace GamaspEngenharia.Controllers
{
    public class HomeController : Controller
    {
        // ---------------------------------------------------------------
        // Páginas
        // ---------------------------------------------------------------

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Servicos()
        {
            return View();
        }

        public ActionResult Galeria()
        {
            return View();
        }

        public ActionResult FaleConosco()
        {
            return View();
        }

        // Rotas antigas mantidas para não quebrar links já indexados.
        public ActionResult RegularizacaoImovel()
        {
            return RedirectPermanent(Url.Action("Servicos") + "#regularizacao");
        }

        public ActionResult Construcao()
        {
            return RedirectToActionPermanent("Servicos");
        }

        public ActionResult Carousel()
        {
            return RedirectToActionPermanent("Index");
        }

        public ActionResult DetalheObra()
        {
            return RedirectToActionPermanent("Galeria");
        }

        public ActionResult DetalheOceans()
        {
            return RedirectToActionPermanent("Galeria");
        }

        public ActionResult DetalheApartamento()
        {
            return RedirectToActionPermanent("Galeria");
        }

        public ActionResult DetalheCozinha()
        {
            return RedirectToActionPermanent("Galeria");
        }

        // ---------------------------------------------------------------
        // Diagnóstico do envio
        // ---------------------------------------------------------------

        /// <summary>
        /// Mostra o estado da configuração de e-mail e o último erro real.
        ///
        /// Só responde se a chave DiagnosticoChave estiver definida no
        /// Web.config e vier igual na querystring. Sem isso, devolve 404 —
        /// nem revela que a página existe. Nunca imprime a senha.
        ///
        /// Uso: /Home/Diagnostico?chave=SUA-CHAVE
        /// </summary>
        [HttpGet]
        public ActionResult Diagnostico(string chave)
        {
            var esperada = ConfigurationManager.AppSettings["DiagnosticoChave"];

            if (string.IsNullOrWhiteSpace(esperada) ||
                !string.Equals(chave, esperada, StringComparison.Ordinal))
            {
                return HttpNotFound();
            }

            var r = new StringBuilder();
            r.AppendLine("DIAGNOSTICO DE ENVIO — Gama SP Engenharia");
            r.AppendLine("gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            r.AppendLine(new string('=', 56));
            r.AppendLine();

            r.AppendLine("CONFIGURACAO");
            r.AppendLine("  ContatoAtivo ........ " + (Ativo() ? "sim" : "NAO (envio desligado)"));
            r.AppendLine("  Destino ............. " + Cfg("ContatoDestino", "(vazio)"));
            r.AppendLine("  Copia (CC) .......... " + Cfg("ContatoCopia", "(nenhuma)"));
            r.AppendLine("  Remetente ........... " + Cfg("ContatoRemetente", "(vazio)"));

            var doAmbiente = Environment.GetEnvironmentVariable("GAMASP_SMTP_SENHA");
            var doConfig = ConfigurationManager.AppSettings["ContatoSenhaApp"];
            var senha = SenhaSmtp();

            r.AppendLine("  Senha configurada ... " + (string.IsNullOrWhiteSpace(senha)
                ? "NAO  <-- e por isto que o envio falha"
                : "sim (" + senha.Length + " caracteres)"));
            r.AppendLine("     variavel de ambiente GAMASP_SMTP_SENHA: " +
                         (string.IsNullOrWhiteSpace(doAmbiente) ? "vazia" : "definida"));
            r.AppendLine("     chave ContatoSenhaApp no Web.config ...: " +
                         (string.IsNullOrWhiteSpace(doConfig) ? "vazia" : "definida"));
            r.AppendLine();

            r.AppendLine("SERVIDOR DE SAIDA (system.net/mailSettings)");
            try
            {
                var secao = ConfigurationManager.GetSection("system.net/mailSettings/smtp")
                            as System.Net.Configuration.SmtpSection;
                if (secao == null)
                {
                    r.AppendLine("  secao ausente");
                }
                else
                {
                    r.AppendLine("  Host ................ " + secao.Network.Host);
                    r.AppendLine("  Porta ............... " + secao.Network.Port);
                    r.AppendLine("  SSL/TLS ............. " + secao.Network.EnableSsl);
                    r.AppendLine("  Usuario ............. " + secao.Network.UserName);
                    r.AppendLine("  Senha no mailSettings " +
                                 (string.IsNullOrEmpty(secao.Network.Password) ? "vazia" : "definida"));
                }
            }
            catch (Exception ex)
            {
                r.AppendLine("  nao foi possivel ler: " + ex.Message);
            }
            r.AppendLine();

            r.AppendLine("APP_DATA");
            r.AppendLine("  " + EstadoAppData());
            r.AppendLine();

            r.AppendLine("ULTIMO ERRO REGISTRADO");
            r.AppendLine(UltimoErro());

            return Content(r.ToString(), "text/plain; charset=utf-8", Encoding.UTF8);
        }

        private static string EstadoAppData()
        {
            try
            {
                var pasta = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(pasta)) return "caminho nao resolvido";

                Directory.CreateDirectory(pasta);

                var teste = Path.Combine(pasta, "escrita-teste.tmp");
                System.IO.File.WriteAllText(teste, "ok");
                System.IO.File.Delete(teste);

                return "gravavel (backup dos contatos e log de erro funcionam)";
            }
            catch (Exception ex)
            {
                return "NAO gravavel: " + ex.Message;
            }
        }

        private static string UltimoErro()
        {
            try
            {
                var pasta = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
                var arquivo = Path.Combine(pasta ?? string.Empty, "erros-envio.txt");

                if (!System.IO.File.Exists(arquivo))
                {
                    return "  nenhum erro registrado ate agora.";
                }

                var linhas = System.IO.File.ReadAllLines(arquivo, Encoding.UTF8);
                var inicio = Math.Max(0, linhas.Length - 25);

                var r = new StringBuilder();
                for (var i = inicio; i < linhas.Length; i++)
                {
                    r.AppendLine("  " + linhas[i]);
                }
                return r.ToString();
            }
            catch (Exception ex)
            {
                return "  nao foi possivel ler o log: " + ex.Message;
            }
        }

        // ---------------------------------------------------------------
        // Formulário de contato
        //
        // Entrega por SMTP autenticado (Gmail, STARTTLS na 587) — mesma
        // abordagem usada no projeto Uechi Labs. A mensagem é sempre gravada
        // em App_Data antes do envio, então nenhum contato se perde mesmo que
        // o SMTP falhe.
        // ---------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EnviaEmail(string nome, string email, string telefone, string servico, string mensagem)
        {
            nome = (nome ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();
            telefone = (telefone ?? string.Empty).Trim();
            servico = (servico ?? string.Empty).Trim();
            mensagem = (mensagem ?? string.Empty).Trim();

            if (nome.Length < 2 || mensagem.Length < 10 || !EmailValido(email))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Por favor, confira o nome, o e-mail e a mensagem antes de enviar."
                });
            }

            var corpo = MontaCorpo(nome, email, telefone, servico, mensagem);

            // Rede de segurança: registra o contato em disco antes de tentar enviar.
            GravaCopiaLocal(corpo);

            if (!Ativo())
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "O envio por e-mail está desativado no momento. Fale conosco pelo WhatsApp: (11) 94743-0418."
                });
            }

            try
            {
                EnviaPorSmtp(nome, email, servico, corpo);

                return Json(new
                {
                    sucesso = true,
                    mensagem = "Mensagem enviada! Retornaremos em até 24 horas úteis."
                });
            }
            catch (Exception ex)
            {
                // O Trace não é legível numa hospedagem compartilhada. Sem isto,
                // qualquer falha de envio vira a mesma frase genérica e não há
                // como descobrir se foi senha errada, porta bloqueada ou rede.
                RegistraFalha(ex);

                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não conseguimos enviar agora. Por favor, chame no WhatsApp (11) 94743-0418 " +
                               "ou escreva para comercial@gamaspengenharia.com.br."
                });
            }
        }

        // ---------------------------------------------------------------
        // Envio
        // ---------------------------------------------------------------

        private static void EnviaPorSmtp(string nome, string email, string servico, string corpo)
        {
            var destino = Cfg("ContatoDestino", "comercial@gamaspengenharia.com.br");
            var copia = Cfg("ContatoCopia", string.Empty);
            var remetente = Cfg("ContatoRemetente", "uodota@gmail.com");
            var remetenteNome = Cfg("ContatoRemetenteNome", "Site Gama SP Engenharia");
            var senha = SenhaSmtp();

            if (string.IsNullOrWhiteSpace(senha))
            {
                throw new InvalidOperationException(
                    "A senha do SMTP não está configurada. Defina a variável de ambiente " +
                    "GAMASP_SMTP_SENHA no servidor ou preencha a chave ContatoSenhaApp " +
                    "no Web.config publicado.");
            }

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(remetente, remetenteNome);
                msg.To.Add(destino);

                // Cópia para acompanhamento. Um endereço inválido na lista não
                // pode impedir a entrega ao destinatário principal.
                foreach (var endereco in copia.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var limpo = endereco.Trim();
                    if (limpo.Length == 0) continue;

                    try
                    {
                        msg.CC.Add(new MailAddress(limpo));
                    }
                    catch (FormatException)
                    {
                        System.Diagnostics.Trace.TraceWarning("ContatoCopia ignorou endereço inválido: " + limpo);
                    }
                }

                // Responder no cliente de e-mail escreve direto para o visitante.
                msg.ReplyToList.Add(new MailAddress(email, nome));

                msg.Subject = "[Site] " + (servico.Length > 0 ? servico : "Contato") + " — " + nome;
                msg.Body = corpo;
                msg.BodyEncoding = Encoding.UTF8;
                msg.SubjectEncoding = Encoding.UTF8;

                // Host, porta e SSL vêm de <system.net><mailSettings> no Web.config.
                using (var smtp = new SmtpClient())
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(remetente, senha);
                    smtp.EnableSsl = true;
                    smtp.Timeout = 20000;
                    smtp.Send(msg);
                }
            }
        }

        private static string MontaCorpo(string nome, string email, string telefone, string servico, string mensagem)
        {
            var corpo = new StringBuilder();
            corpo.AppendLine("Novo contato pelo site gamaspengenharia.com.br");
            corpo.AppendLine("------------------------------------------------");
            corpo.AppendLine("Nome:     " + nome);
            corpo.AppendLine("E-mail:   " + email);
            corpo.AppendLine("Telefone: " + (telefone.Length > 0 ? telefone : "(não informado)"));
            corpo.AppendLine("Serviço:  " + (servico.Length > 0 ? servico : "(não informado)"));
            corpo.AppendLine("Recebido: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            corpo.AppendLine("------------------------------------------------");
            corpo.AppendLine();
            corpo.AppendLine(mensagem);
            corpo.AppendLine();
            corpo.AppendLine("Dica: responda a este e-mail para falar direto com quem enviou.");
            return corpo.ToString();
        }

        /// <summary>
        /// Guarda uma cópia de cada contato em App_Data. Se o SMTP cair, o lead
        /// continua registrado no servidor.
        /// </summary>
        private static void GravaCopiaLocal(string corpo)
        {
            try
            {
                var pasta = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(pasta)) return;

                Directory.CreateDirectory(pasta);

                var arquivo = Path.Combine(pasta, "contatos-" + DateTime.Now.ToString("yyyy-MM") + ".txt");
                var registro = new StringBuilder();
                registro.AppendLine("================================================");
                registro.Append(corpo);
                registro.AppendLine();

                System.IO.File.AppendAllText(arquivo, registro.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Nunca deixa a falha do log impedir o envio.
                System.Diagnostics.Trace.TraceWarning("Não foi possível gravar a cópia local do contato: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------
        // Auxiliares
        // ---------------------------------------------------------------

        /// <summary>
        /// A senha vem preferencialmente da variável de ambiente, para não
        /// precisar ficar no Web.config (que está num repositório público).
        /// </summary>
        private static string SenhaSmtp()
        {
            var senha = Environment.GetEnvironmentVariable("GAMASP_SMTP_SENHA");
            if (string.IsNullOrWhiteSpace(senha))
            {
                senha = ConfigurationManager.AppSettings["ContatoSenhaApp"];
            }
            return senha;
        }

        /// <summary>
        /// Grava o erro real em App_Data/erros-envio.txt. Fica fora do alcance
        /// do navegador (o IIS bloqueia App_Data) e pode ser lido por FTP.
        /// </summary>
        private static void RegistraFalha(Exception ex)
        {
            try
            {
                var pasta = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(pasta)) return;

                Directory.CreateDirectory(pasta);

                var texto = new StringBuilder();
                texto.AppendLine("================================================");
                texto.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                texto.AppendLine(ex.GetType().FullName + ": " + ex.Message);

                var interna = ex.InnerException;
                while (interna != null)
                {
                    texto.AppendLine("  -> " + interna.GetType().FullName + ": " + interna.Message);
                    interna = interna.InnerException;
                }

                var smtpEx = ex as SmtpException;
                if (smtpEx != null)
                {
                    texto.AppendLine("  StatusCode: " + smtpEx.StatusCode);
                }

                texto.AppendLine(ex.StackTrace);
                texto.AppendLine();

                System.IO.File.AppendAllText(
                    Path.Combine(pasta, "erros-envio.txt"), texto.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Registrar a falha nunca pode gerar outra falha.
            }
        }

        private static bool Ativo()
        {
            bool ativo;
            var valor = ConfigurationManager.AppSettings["ContatoAtivo"];
            return !bool.TryParse(valor, out ativo) || ativo;
        }

        private static string Cfg(string chave, string padrao)
        {
            var valor = ConfigurationManager.AppSettings[chave];
            return string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim();
        }

        private static bool EmailValido(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;
            try
            {
                var addr = new MailAddress(valor);
                return addr.Address == valor && valor.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
