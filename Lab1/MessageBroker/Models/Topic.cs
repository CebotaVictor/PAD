using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Topic
    {
        [Key]
        public string? Name { get; set; }
    }
}
