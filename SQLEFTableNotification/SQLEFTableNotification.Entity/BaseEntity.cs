using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLEFTableNotification.Entity
{
    public class BaseEntity:IEntityPk
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        //[ConcurrencyCheck]
        //[Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
