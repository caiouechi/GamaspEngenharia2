using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace CaioUechi.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Carousel()
        {
            return View();
        }

        public ActionResult Construcao()
        {
            return View();
        }

        public ActionResult Servicos()
        {
            return View();
        }

        public ActionResult DetalheObra()
        {
            return View();
        }

        public ActionResult DetalheOceans()
        {
            return View();
        }

        public ActionResult DetalheApartamento()
        {
            return View();
        }

        public ActionResult DetalheCozinha()
        {
            return View();
        }

        public ActionResult FaleConosco()
        {
            return View();
        }

        public ActionResult Galeria()
        {
            return View();
        }

        public ActionResult Construcao3()
        {
            return View();
        }

        [HttpPost]
        public ActionResult EnviaEmail(string destinatario, string telefone, string mensagem, string name)
        {
            bool retorno;

            try
            {
                //'cria objeto para receber os dados do email
                MailMessage oEmail = new MailMessage();

                //'remetente do email
                oEmail.From = new MailAddress("caio.uechi@gmail.com");
                //'destinatario do email
                oEmail.To.Add("caio_uechi@hotmail.com");

                //'destinatario de copia do email
                //oEmail.To.Add("diretoria@takaki.com.br");
                //'destinatario de copia oculta
                //oEmail.Bcc.Add("copiaOculta");
                //'prioridade de envio
                oEmail.Priority = MailPriority.Normal;
                //'define o assunto do email
                oEmail.Subject = name + " - " + destinatario;
                //'define a mensagem principal do email
                mensagem = mensagem + "Telefone: " + telefone;
                oEmail.Body = mensagem;

                //'cria o objeto SMTP
                SmtpClient oSmtp = new SmtpClient();

                oSmtp.EnableSsl = true;

                try
                {
                    //'envia o email
                    oSmtp.Send(oEmail);
                }
                catch (Exception e)
                {
                    var b = e.InnerException.ToString();
                }

                retorno = true;
            }
            catch (Exception e)
            {
                var b = e.Message;
                retorno = false;
            }


            return new JsonResult { Data = retorno, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}
