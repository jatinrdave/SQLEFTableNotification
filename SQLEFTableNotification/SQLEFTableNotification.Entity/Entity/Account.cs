using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SQLEFTableNotification.Entity
{
    /// <summary>
    /// A account with users
    /// </summary>
    public class Account : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string Name { get; set; }
        [Required]
        [StringLength(30)]
        public string Email { get; set; }
        [StringLength(255)]
        public string Description { get; set; }
        public bool IsTrial { get; set; }
        public bool IsActive { get; set; }
        public DateTime SetActive { get; set; }

        public virtual ICollection<User> Users { get; set; }

    }




}
