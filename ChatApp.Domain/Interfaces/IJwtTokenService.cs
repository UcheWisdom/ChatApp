using System;
using System.Collections.Generic;
using System.Text;

using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}