using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;// JWT token
        public string Role { get; set; } = string.Empty;  // added role string
        public string FullName { get; set; } = string.Empty;
    }
}
