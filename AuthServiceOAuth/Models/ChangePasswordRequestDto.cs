﻿using System.ComponentModel.DataAnnotations;

namespace OAuthAuthService.Models
{
    public class ChangePasswordRequestDto
    {
   
        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}