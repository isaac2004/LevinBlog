﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LevinBlog.Database.Entity
{
    [Table("Contacts")]
    public class ContactEntity
    {
        [Key]
        public int ContactId { get; set; }

        [MaxLength(254)]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
    }
}