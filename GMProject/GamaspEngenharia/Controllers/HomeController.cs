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
                System.Diagnostics.Trace.TraceError("Falha ao enviar e-mail de contato: " + ex);

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
                    // A senha pode vir de variável de ambiente, que tem prioridade
                    // sobre o Web.config — assim ela não precisa ficar no repositório.
                    var senha = Environment.GetEnvironmentVariable("GAMASP_SMTP_SENHA");
                    if (string.IsNullOrWhiteSpace(senha))
                    {
                        senha = ConfigurationManager.AppSettings["ContatoSenhaApp"];
                    }

                    if (!string.IsNullOrWhiteSpace(senha))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(remetente, senha);
                    }

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
