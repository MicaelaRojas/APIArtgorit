using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ApiMysql.Models
{
    [Table("obras")]
    public class Obras
    {
        [Key]
        public int id { get; set; }
        public string titulo { get; set; }
        public string descripcion { get; set; }
        public string imagen { get; set; }
        public string codigo { get; set; }

        // Agrega la propiedad correspondiente al modelo
        public int user_id { get; set; }
    }
}

