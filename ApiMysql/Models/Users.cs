using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ApiMysql.Models
{
    [Table("users")]
    public class Users
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string rol { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }
}
