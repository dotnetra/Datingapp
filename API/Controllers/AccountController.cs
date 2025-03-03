﻿using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers
{
	public class AccountController:BaseApiController
	{
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context,ITokenService tokenService)
		{
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
           
            //We are using ActionResult that's why we are able to using different different http request code for example 400 badrequest, 500 etc
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

            //"using" statement is used , it disposed that class correctly, that class has disposed method which we must call.
            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            //below statement tracking entity , it is not doing database operation.
            _context.Users.Add(user);

            //below statement insert in to db.
            await _context.SaveChangesAsync();

            return new UserDto {
                Username = user.UserName,
                Token=_tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username) { 
        
            return await _context.Users.AnyAsync(x => x.UserName==username.ToLower());
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        { 
            var user= await _context.Users.SingleOrDefaultAsync(x => x.UserName== loginDto.username);

            if (user == null) return Unauthorized("Invalid username");

            using var hmac= new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if(computedHash[i]!= user.PasswordHash[i]) return Unauthorized("Invalid password");
            }
            return new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }
	}
}