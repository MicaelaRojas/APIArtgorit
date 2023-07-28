using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ApiMysql.Models
{
    [Table("configuracion_correo")]
    public class Configuracion_correo
    {
        [Key]
        public int idCorreo { get; set; }
        public string senderEmail { get; set; }
        public string senderName { get; set; }
        public string senderPassword { get; set; }
        public string smtpHost { get; set; }
        public string smtpServer { get; set; }
        public int smtpPort { get; set; }
        public bool enableSsl { get; set; }
    }
}
