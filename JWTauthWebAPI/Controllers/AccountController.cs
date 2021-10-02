using JWTauthWebAPI.Data;
using JWTauthWebAPI.Helpers;
using JWTauthWebAPI.Model;
using JWTauthWebAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JWTauthWebAPI.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext db, IConfiguration configuration, IEmailSender emailSender)
        {
            _db = db;
            _configuration = configuration;
            _emailSender = emailSender;
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserAccount userAccount)
        {
            if (userAccount != null && !string.IsNullOrEmpty(userAccount.Email) &&
                !string.IsNullOrEmpty(userAccount.Password))
            {
                var passwordDetails = PasswordProtector.GetHashAndSalt(userAccount.Password);
                userAccount.Password = passwordDetails.HashText;
                userAccount.PasswordSalt = passwordDetails.SaltText;
                userAccount.HashIteration = passwordDetails.HashIteration;
                userAccount.HashLength = passwordDetails.HashLength;
                userAccount.CreatedDate = DateTime.Now;
                userAccount.Version = 1;
                userAccount.UserName = userAccount.Email;
                if (userAccount.Role == null)
                {
                    userAccount.Role = Role.User;
                }

                _db.UserAccounts.Add(userAccount);
                await _db.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Required data for user registeration not found");
            }

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(SignInModel signInModel)
        {
            if (signInModel != null && !string.IsNullOrEmpty(signInModel.UserName) && !string.IsNullOrEmpty(signInModel.Password))
            {
                var user = await _db.UserAccounts.FirstOrDefaultAsync(x => x.Email.Equals(signInModel.UserName));
                if (user != null && !string.IsNullOrEmpty(user.Password) && !string.IsNullOrEmpty(user.PasswordSalt))
                {
                    bool passwordVerification = PasswordProtector.VerifyPassword
                        (signInModel.Password,
                        user.Password,
                        user.PasswordSalt,
                        user.HashLength
                        , user.HashIteration);

                    if (passwordVerification)
                    {
                        string jwtToken = JwtTokenGenerator.GetJwtToken(user, _configuration);

                        return Ok(new UserAccount
                        {
                            UserName = user.UserName,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            Password = "",
                            Role = user.Role,
                            UserAccountId = user.UserAccountId,
                            Token = jwtToken
                        });
                    }
                    else
                    {
                        return Unauthorized("User authentication failed. Please enter valid user account details for sign in.");
                    }
                }
                else
                {
                    return NotFound("User not found. Please enter valid user name");
                }

            }
            else
            {
                return BadRequest("Required data for user sign in not found");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(PasswordChangeModel model)
        {
            if(model==null)
            {
                return BadRequest("Empty Operation");
            }

            var data = _db.UserAccounts.Where(a => a.UserAccountId == model.UserID && a.UserName == model.UserName);
            if (data == null)
            {
                return NotFound("Not Found");
            }
            var passwordDetails = PasswordProtector.GetHashAndSalt(model.Password);
            UserAccount userAccount = new UserAccount();
            userAccount.Password = passwordDetails.HashText;
            userAccount.PasswordSalt = passwordDetails.SaltText;
            userAccount.HashIteration = passwordDetails.HashIteration;
            userAccount.HashLength = passwordDetails.HashLength;
            userAccount.UpdateDate = DateTime.Now;
            _db.Update(userAccount);
            await _db.SaveChangesAsync();
            return Ok("Password changed successfull");

        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgetPassword(RecoverPasswordModel recover)
        {

            if (!string.IsNullOrEmpty(recover.OTP))
            {
                var otpObj = await _db.OTPServices.OrderByDescending(x => x.ID).FirstOrDefaultAsync(x => x.Email.Equals(recover.Email));
                if (otpObj == null)
                {
                    return BadRequest("Invalid OPT");
                }

                if (otpObj.OTP == recover.OTP)
                {
                    bool verify = false;
                    DateTime otpTime = otpObj.OTPTime;
                    DateTime Now = DateTime.UtcNow;
                    var duration = Now.Subtract(otpTime).TotalSeconds;
                    if (duration <= 300)
                    {
                        verify = true;
                    }
                    else
                    {
                        verify = false;
                    }
                    if (verify)
                    {
                        if (!string.IsNullOrEmpty(recover.Email) && !string.IsNullOrEmpty(recover.NewPassword))
                        {
                            var passwordDetails = PasswordProtector.GetHashAndSalt(recover.NewPassword);
                            UserAccount userAccount = new UserAccount();
                            userAccount.Password = passwordDetails.HashText;
                            userAccount.PasswordSalt = passwordDetails.SaltText;
                            userAccount.HashIteration = passwordDetails.HashIteration;
                            userAccount.HashLength = passwordDetails.HashLength;
                            userAccount.UpdateDate = DateTime.Now;
                            _db.UserAccounts.Update(userAccount);
                            _db.OTPServices.RemoveRange(_db.OTPServices.Where(x => x.UserAccountId == otpObj.UserAccountId));
                            await _db.SaveChangesAsync();
                            return Ok("Password reset successfull");
                        }
                    }
                    else
                    {
                        return BadRequest("OTP Time Out");

                    }
                }
            }

            //

            if (string.IsNullOrEmpty(recover.Email))
            {
                return BadRequest("Please Provide Valid Email");
            }
            else
            {
                if (recover != null && !string.IsNullOrEmpty(recover.Email))
                {
                    var user = await _db.UserAccounts.FirstOrDefaultAsync(x => x.UserName.Equals(recover.Email));
                    if (user != null && !string.IsNullOrEmpty(user.Password))
                    {
                        string OTP = string.Empty;
                        var bytes = Base32Encoding.ToBytes("JBSWY3DPEHPK3PXP");
                        var totp = new Totp(bytes, step: 300);
                        OTP = totp.ComputeTotp(DateTime.UtcNow);
                        //
                        System.Text.StringBuilder sb = new();
                        sb.Append("<h3>MR. " + user.LastName + ",</h3>");
                        sb.Append("<div>");
                        sb.Append("<p> We have received a request to reset your JWTAUTH Project password.");
                        sb.Append("To reset your password please use the mentioned six digit code:</p>");
                        sb.Append("<h2>" + OTP + "</h2>");
                        sb.Append("<p>" + "If you did not request a password reset, please ignore this email. OTP will expire in 5 minutes." + "</p><br/>");
                        sb.Append("<p>" + "Thank you," + "</p>");
                        sb.Append("<p>" + "JWT Auth Team" + "</p>");
                        var sbbody = sb.ToString();

                        await _emailSender.SendEmailAsync(user.Email, "Password Recover JWT AUTH Project", sbbody);
                        OTPService oTPService = new();
                        oTPService.Email = user.Email;
                        oTPService.OTP = OTP;
                        oTPService.UserAccountId = user.UserAccountId;
                        oTPService.OTPTime = DateTime.UtcNow;
                        _db.OTPServices.Add(oTPService);
                        await _db.SaveChangesAsync();

                        return Ok(new RecoverPasswordModel
                        {
                            Email = recover.Email,
                            OTP = "Check Your Email :" + user.Email + " . within 5 minutes to verify."
                        });
                    }
                }
                else
                {
                    return NotFound("NO user found");
                }

            }
            return Ok();
        }
    }
}
