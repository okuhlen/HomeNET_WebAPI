﻿using Microsoft.AspNetCore.Mvc;
using HomeNetAPI.Repository;
using HomeNetAPI.Models;
using AutoMapper;
using HomeNetAPI.ViewModels;
using System;
using System.Threading.Tasks;
using HomeNetAPI.Services;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using MailKit.Net.Smtp;
using System.Collections.Generic;
using MailKit;
using MimeKit;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace HomeNetAPI.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private IUserRepository userRepository;
        private IMapper mapper;
        private ICryptography crypto;
        //ASP.NET Core Identity
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly string androidClient = "bab9baac6fac05ac083c5f42ec25d76d";
        private String mailPassword = "Okuhle*1994";
        private IHouseRepository houseRepository;
        private IHouseMemberRepository houseMemberRepository;
        private IHousePostRepository housePostRepository;

        public UserController(IUserRepository userRepository, IMapper mapper, ICryptography crypto, UserManager<User> userManager, SignInManager<User> signInManager, IPasswordHasher<User> passwordHasher, IHouseRepository houseRepository, IHouseMemberRepository houseMemberRepository, IHousePostRepository housePostRepository)
        {
            this.houseMemberRepository = houseMemberRepository;
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.crypto = crypto;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.passwordHasher = passwordHasher;
            this.houseRepository = houseRepository;
            this.housePostRepository = housePostRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfilePicture([FromQuery] String emailAddress, [FromQuery] String clientCode)
        {
            SingleResponse<FileStream> response = new SingleResponse<FileStream>();
            try
            {
                if (androidClient != clientCode)
                {
                    response.DidError = true;
                    response.Message = "Please send valid credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }
                var foundUser = await userManager.FindByEmailAsync(emailAddress);
                if (foundUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user found with provided data";
                    response.Model = null;
                    return NotFound(response);
                }
                if (foundUser.ProfileImage != null)
                {
                    var fileStream = System.IO.File.Open(foundUser.ProfileImage, System.IO.FileMode.Open, FileAccess.Read);
                    return File(fileStream, "image/jpeg");
                } else
                {
                    response.DidError = true;
                    response.Message = "No profile picture found";
                    response.Model = null;
                    return NotFound(response);
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message + "\n" + error.StackTrace;
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture([FromQuery] String emailAddress, [FromForm] IFormFile imageFile, [FromQuery] String clientCode) 
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            try
            {
                if (clientCode != androidClient)
                {
                    response.DidError = true;
                    response.Message = "Please send valid client credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }
                var foundUser = await userManager.FindByEmailAsync(emailAddress);
                if (foundUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user was found with the provided data";
                    response.Model = null;
                    return NotFound(response);
                }
                if (imageFile == null)
                {
                    response.DidError = true;
                    response.Message = "No picure was found. Please try again";
                    response.Model = null;
                    return NotFound(response);
                }
                if (imageFile.ContentType != "image/jpeg")
                {
                    response.DidError = true;
                    response.Message = "Only image files are accepted by the server";
                    response.Model = null;
                    return BadRequest(response);
                }
                String newDirectory = $"C:/HomeNET/Users/{foundUser.Id}";
                String finalFileName = "";
                if (!Directory.Exists(newDirectory))
                {
                    Directory.CreateDirectory(newDirectory);
                }
                if (imageFile.FileName.Contains(":"))
                {
                    finalFileName = GenerateRandomString() + imageFile.FileName.Replace(":", "_");
                } else
                {
                    finalFileName = GenerateRandomString () +imageFile.FileName;
                }
                using (var fileStream = new FileStream(newDirectory +"/"+finalFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    await imageFile.CopyToAsync(fileStream);
                    String oldFile = foundUser.ProfileImage;
                    foundUser.ProfileImage = newDirectory + "/" + finalFileName;
                    var updatedUser = await Task.Run(() =>
                    {
                        return userRepository.UpdateUserAccount(foundUser);
                    });
                    if (oldFile != null) {
                        System.IO.File.Delete(oldFile);
                    }
                    if (updatedUser != null) {
                        response.DidError = false;
                        response.Message = "Profile picture saved!";
                        response.Model = updatedUser;
                        return Ok(response);

                    } else
                    {
                        response.DidError = true;
                        response.Model = foundUser;
                        response.Message = "Error saving profile picure";
                        return BadRequest(response);
                    }
 
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message + "\n" + error.StackTrace;
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginViewModel loginViewModel, [FromQuery] string clientCode)
        {
            SingleResponse<User> response = new SingleResponse<User>();
            try
            {
                if (clientCode == androidClient)
                {
                    if (loginViewModel.Username!= null && loginViewModel.Password != null)
                    {
                        var resultUser = await userManager.FindByNameAsync(loginViewModel.Username);
                        if (resultUser != null)
                        {
                            
                            if (passwordHasher.VerifyHashedPassword(resultUser, resultUser.PasswordHash, loginViewModel.Password) == PasswordVerificationResult.Success)
                            {
                                response.DidError = false;
                                response.Message = "Login Successfull";
                                response.Model = resultUser;
                                return Ok(response);
                            }
                            else
                            {
                                response.DidError = true;
                                response.Message = "Invalid Password. Please try again";
                                response.Model = null;
                                return BadRequest(response);
                            }
                        }
                        else
                        {
                            response.DidError = true;
                            response.Message = "Invalid username. Please try again";
                            response.Model = null;
                            return NotFound(response);
                        }
                    }
                    else
                    {
                        response.DidError = true;
                        response.Message = "Please send a valid username and password";
                        response.Model = null;
                        return BadRequest(response);
                    }
                }
                else
                {
                    response.DidError = true;
                    response.Message = "Please send a valid client code to the server";
                    response.Model = null;
                    return BadRequest(response);
                }
            }
            catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message;
                response.Model = null;
                return BadRequest(response);
            }


        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User newUser, [FromQuery] string clientCode)
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            if (clientCode == androidClient)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        if (newUser.Name.ToUpper().Trim() == newUser.UserName.ToUpper().Trim())
                        {
                            response.DidError = true;
                            response.Message = "Your username cannot be the same as your name. Please try again";
                            response.Model = newUser;
                            return BadRequest(response);
                        }

                        if (newUser.Password.ToUpper().Trim() == newUser.Name.ToUpper().Trim())
                        {
                            response.DidError = true;
                            response.Message = "The password cannot be the same as your name. Please try again";
                            response.Model = newUser;
                            return BadRequest(response);
                        }

                        if (newUser.Password.ToUpper().Trim().Contains(newUser.Name.ToUpper().Trim()))
                        {
                            response.DidError = true;
                            response.Message = "The entered password cannot contain any part of your name. Please try again";
                            response.Model = newUser;
                            return BadRequest(response);
                        }

                        //Should we need to worry about salting the password? Or does Identity do this for us?
                        var createdUser = await Task.Run(() =>
                        {
                            return userManager.CreateAsync(newUser, newUser.Password);
                        });
                        if (createdUser.Succeeded)
                        {
                            response.DidError = false;
                            response.Message = "Account created successfully! Welcome to HomeNET!";
                            response.Model = newUser;
                            return Ok(response);
                        }
                        else
                        {
                            response.DidError = true;
                            response.Message = "Error(s) occurred with registration: \n";
                            foreach (IdentityError result in createdUser.Errors)
                            {
                                response.Message = response.Message + "\n" + result.Description;
                            }
                            response.Model = newUser;
                            return BadRequest(response);
                        }
                    }
                    else
                    {
                        response.DidError = true;
                        response.Message = "Error Processing new user. Please ensure all data fields have valid data";
                        return BadRequest(response);
                    }
                }
                catch (Exception error)
                {
                    response.DidError = true;
                    response.Message = error.Message;
                    return BadRequest(response);
                }

            }
            else
            {
                response.DidError = true;
                response.Message = "Please ensure you send a valid client code to the server";
                response.Model = null;
                return BadRequest(response);
            }

        }
        
        [HttpGet]
        public async Task<IActionResult> GetUser([FromQuery] string emailAddress, [FromQuery] string clientCode)
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            if (clientCode == androidClient)
            {
                try
                {
                    var resultUser = await Task.Run(() =>
                    {
                        return userRepository.GetUser(emailAddress);
                    });
                    if (resultUser == null)
                    {
                        response.DidError = true;
                        response.Message = $"The email address {emailAddress} returned no matching account";
                        response.Model = null;
                        return NotFound(response);
                    }
                    else
                    {
                        response.DidError = false;
                        response.Message = $"User with E-Mail address {emailAddress} returned the following record";
                        response.Model = resultUser;
                        return Ok(response);
                    }
                }
                catch (Exception error)
                {
                    response.DidError = true;
                    response.Message = error.Message;
                    response.Model = null;
                    return BadRequest(response);
                }
            }
            else
            {
                response.DidError = true;
                response.Message = "Please send a valid client code to the server";
                response.Model = null;
                return BadRequest(response);
            }

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromQuery] string emailAddress)
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            if (emailAddress != null)
            {
                var resultUser = await Task.Run(() =>
                {
                    return userRepository.GetUser(emailAddress);
                });
                if (resultUser == null)
                {
                    response.DidError = true;
                    response.Message = $"No matching user account was found for the email address {emailAddress}";
                    response.Model = null;
                    return NotFound(response);
                }
                else
                {
                    var deleteUser = await Task.Run(() =>
                    {
                        return userRepository.DeleteUser(resultUser.Id);
                    });

                    if (deleteUser.IsDeleted == 1)
                    {
                        response.DidError = false;
                        response.Message = $"User {deleteUser.Name} {deleteUser.Surname} has successfully been deleted from our system records";
                        response.Model = deleteUser;
                        return Ok(response);
                    }
                    else
                    {
                        response.DidError = true;
                        response.Model = deleteUser;
                        response.Message = "Error deletring the user";
                        return BadRequest(response);
                    }
                }
            }

            else
            {
                response.DidError = true;
                response.Message = "Please ensure you are supplying a valid email address";
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateToken([FromBody] LoginViewModel loginViewModel, [FromQuery] String clientCode)
        {
            SingleResponse<Object> response = new SingleResponse<Object>();
            if (clientCode == androidClient)
            {
                try
                {
                    var currentUser = await userManager.FindByNameAsync(loginViewModel.Username);
                    if (currentUser != null)
                    {
                        if (passwordHasher.VerifyHashedPassword(currentUser, currentUser.PasswordHash, loginViewModel.Password) == PasswordVerificationResult.Success)
                        {
                            //If the username and password are fine, create the password
                            var claimObject = new[]
                            {
                        new Claim(JwtRegisteredClaimNames.Sub, currentUser.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.GivenName, currentUser.Name),
                        new Claim(JwtRegisteredClaimNames.Email, currentUser.Email),
                        new Claim(JwtRegisteredClaimNames.FamilyName, currentUser.Surname)
                    };

                            var certificateFile = new X509Certificate2("Certificates/homenet.pfx", "Okuhle*1994");
                            var key = new SymmetricSecurityKey(certificateFile.GetPublicKey());
                            String secret = Convert.ToBase64String(certificateFile.GetPublicKey());

                            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                            var token = new JwtSecurityToken(
                                issuer: "https://www.homenet.net.za",
                                audience: "https://www.homenet.net.za",
                                claims: claimObject,
                                expires: DateTime.Now.AddDays(20),
                                signingCredentials: signingCredentials);

                            response.DidError = false;
                            response.Message = "Here is your token";
                            response.Model = (Token)new Token()
                            {
                                TokenHandler = new JwtSecurityTokenHandler().WriteToken(token),
                                Expires = token.ValidTo
                            };

                            return Ok(response);

                        }
                        else
                        {
                            response.DidError = true;
                            response.Message = "No user was returned with the supplied username and/or password. Please try again";
                            return BadRequest(response);
                        }
                    }
                    else
                    {
                        response.DidError = true;
                        response.Message = "No user has been found, given the username and/or password. Please try again";
                        response.Model = (String)"";
                        return BadRequest(response);
                    }

                }
                catch (Exception error)
                {
                    response.DidError = true;
                    response.Message = error.Message;
                    return BadRequest(response);
                }
            }
            else
            {

                response.DidError = true;
                response.Message = "Please ensure you send the correct client ID to the server";
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpDelete]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteRegistration([FromQuery] int userId, [FromQuery]String clientCode)
        {
            SingleResponse<User> response = new SingleResponse<User>();
            if (clientCode == androidClient)
            {
                try
                {
                    var result = await Task.Run(() =>
                    {
                        return userRepository.RemoveUser(userId);
                    });
                    if (result == null)
                    {
                        response.DidError = true;
                        response.Message = $"No user with the provided id {userId} was found on the system";
                        response.Model = null;
                        return NotFound(response);
                    }
                    else
                    {
                        response.DidError = false;
                        response.Message = "User registration has been reverted (removed from the system)";
                        response.Model = result;
                        return Ok(response);

                    }

                }
                catch (Exception error)
                {
                    response.DidError = true;
                    response.Message = error.Message;
                    response.Model = null;
                    return BadRequest(response);
                }
            }
            else
            {
                response.DidError = true;
                response.Message = "Please ensure you send a valid client code to the server";
                response.Model = null;
                return BadRequest(response);
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ForgotPassword([FromQuery] String emailAddress, [FromQuery] String dateOfBirth, [FromQuery] String clientCode)
        {   //Source: http://dotnetthoughts.net/how-to-send-emails-from-aspnet-core/ 
            SingleResponse<User> response = new SingleResponse<Models.User>();
            try
            {
                if (clientCode == androidClient)
                {
                    //dddd/mm/yy                  
                    var resultUser = await Task.Run(() =>
                    {
                        return userRepository.GetUser(emailAddress, dateOfBirth);
                    });
                    if (resultUser != null)
                    {
                        String resetToken = await userManager.GeneratePasswordResetTokenAsync(resultUser);
                        String randomPassword = GenerateRandomPassword();
                        var changeResult = await userManager.ResetPasswordAsync(resultUser, resetToken, randomPassword);
                        if (changeResult.Succeeded)
                        {//Proceed with emailing the new password
                            
                            var emailMessage = new MimeMessage();
                            emailMessage.From.Add(new MailboxAddress("Okuhle Ngada (HomeNET System)", "okuhle.ngada@koeksworld.com"));
                            emailMessage.To.Add(new MailboxAddress($"{resultUser.Name} ({resultUser.UserName})", resultUser.Email));
                            emailMessage.Subject = "HomeNET Forgot Password Reset";
                            emailMessage.Body = new TextPart("plain")
                            {
                                Text = $"Hi {resultUser.Name},\n\nWe received your request to reset your password.\nYour new password is: {randomPassword}\n\nPlease login and change your password.\n\nKind Regards,\nHomeNET Team"
                            };
                            var emailClient = new SmtpClient();
                            emailClient.Connect("mail.koeksworld.com", 465, true);
                            emailClient.Authenticate("okuhle.ngada@koeksworld.com", mailPassword);
                            emailClient.AuthenticationMechanisms.Remove("XOAUTH2");
                            emailClient.Send(emailMessage);
                            emailClient.Disconnect(true);
                            response.DidError = false;
                            response.Message = "Password reset successfully! Please check your emails for your new password!";
                            response.Model = null;
                            return Ok(response);
                        }
                        else
                        {
                            response.DidError = true;
                            response.Message = $"An error occurred while resetting your password";
                            response.Model = null;
                            return BadRequest(response);
                        }
                    }
                    else
                    {
                        response.DidError = true;
                        response.Message = "The email address and/or date of birth entered does not match any system records";
                        response.Model = null;
                        return NotFound(response);
                    }
                }
                else
                {
                    response.DidError = true;
                    response.Message = "Please send a valid client code to the server";
                    response.Model = null;
                    return BadRequest(response);
                }
            }
            catch (Exception error)
            {
                response.DidError = false;
                response.Message = error.Message;
                response.Model = null;
                return BadRequest(response);
            }
        }

        private String GenerateRandomPassword()
        {
            char[] letterArray = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            char[] symbolArray = { '!', '#', '@', '*' };
            Random random = new Random();
            String finalString = "";
            for (int a = 0; a < 12; a++)
            {
                if (a == 0)
                {
                    finalString = finalString + letterArray[random.Next(0, 25)].ToString();
                }
                else if (a > 0 && a < 5)
                {
                    finalString = finalString + letterArray[random.Next(0, 25)].ToString().ToLower();
                }
                else if (a > 5 && a < 7)
                {
                    finalString = finalString + random.Next(1, 9);
                }
                else
                {
                    finalString = finalString + symbolArray[random.Next(0, 3)].ToString();
                }
            }
            return finalString;
        }

        [HttpGet]
        ///Get all house posts for all houses a user may be subscribed to. 
        public async Task<IActionResult> GetHousePosts([FromQuery] string emailAddress, [FromQuery] String clientCode)
        {
            ListResponse<HousePost> response = new ListResponse<HousePost>();
            try
            {
                if (clientCode != androidClient)
                {
                    response.DidError = true;
                    response.Message = "Please send valid client credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }

                var selectedUser = await userManager.FindByEmailAsync(emailAddress);
                if (selectedUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user was found with the selected credentials";
                    response.Model = null;
                    return NotFound(response);
                }

                var subscribedHouses = await Task.Run(() =>
                {
                    return houseMemberRepository.GetHouseMember(selectedUser.Id);
                });
                if (subscribedHouses == null)
                {
                    response.DidError = true;
                    response.Message = "No subscriptions to houses were found for the selected user. Please consider joining a house, or creating a house";
                    response.Model = null;
                    return NotFound(response);
                }
                List<HousePost> housePostList = new List<HousePost>();

                foreach (HouseMember membership in subscribedHouses)
                {
                    var result = await Task.Run(() =>
                    {
                        return housePostRepository.GetHousePosts(membership.HouseID);
                    });
                    if (result != null)
                    {
                        foreach (HousePost post in result)
                        {
                            housePostList.Add(post);
                        }
                        result = null;
                    }
                }

                if (housePostList.Count > 0)
                {
                    response.DidError = false;
                    response.Message = "Here are found houses";
                    response.Model = housePostList;
                    return Ok(response);
                } else
                {
                    response.DidError = true;
                    response.Message = "No house posts were found in all houses the user may be subscribed to";
                    response.Model = null;
                    return NotFound(response);
                }
            }
            catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message + "\n"+error.StackTrace;
                response.Model = null;
                return BadRequest(response);
          }
        }

        [HttpPost] 
        public async Task<IActionResult> RegisterFirebaseToken([FromForm] String token, [FromForm] String emailAddress, [FromQuery] String clientCode)
        {
            SingleResponse<String> response = new SingleResponse<string>();
            try
            {
                if (androidClient != clientCode)
                {
                    response.DidError = true;
                    response.Message = "Please send a valid client code to the server";
                    response.Model = null;
                    return BadRequest(response);
                }

                if (token == null || emailAddress == null)
                {
                    response.DidError = true;
                    response.Message = "Please send token data and user data before registering";
                    response.Model = null;
                    return BadRequest(response);
                }

                var selectedUser = await userManager.FindByEmailAsync(emailAddress);
                if (selectedUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user has been found with the provided data";
                    response.Model = token;
                    return NotFound(response);
                }

                selectedUser.FirebaseMessagingToken = token;
                var updateTask = await userManager.UpdateAsync(selectedUser);
                if (updateTask.Succeeded)
                {
                    response.DidError = false;
                    response.Message = "Firebase token saved successfully for the selected user";
                    response.Model = token;
                    return Ok(response);
                } else
                {
                    response.DidError = true;
                    response.Message = "Something went wrong with saving the data";
                    response.Model = null;
                    return BadRequest(response);
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message;
                response.Model = error.StackTrace;
                return BadRequest(response);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserById([FromQuery] int userId, [FromQuery] String clientCode)
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            try
            {
                if (clientCode != androidClient)
                {
                    response.DidError = true;
                    response.Message = "Please send valid client credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }

                var userResult = await userManager.FindByIdAsync(Convert.ToString(userId));
                if (userResult != null)
                {
                    response.DidError = false;
                    response.Message = "Here is the matching user";
                    response.Model = userResult;
                    return Ok(response);
                } else
                {
                    response.DidError = true;
                    response.Message = "No user found with the provided data";
                    response.Model = null;
                    return NotFound(response);
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message;
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserMemberships([FromQuery] String emailAddress, [FromQuery] String clientCode)
        {
            ListResponse<HouseMember> response = new ListResponse<HouseMember>();
            try
            {
                if (clientCode != androidClient)
                {
                    response.DidError = true;
                    response.Message = "Please send valid client credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }

                var selectedUser = await userManager.FindByEmailAsync(emailAddress);
                if (selectedUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user was found with the selected user credentials";
                    response.Model = null;
                    return BadRequest(response);
                }

                var selectedMemberships = await Task.Run(() =>
                {
                    return houseMemberRepository.GetHouseMember(selectedUser.Id);
                });
                if (selectedMemberships != null)
                {
                    response.DidError = false;
                    response.Message = $"Here are memberships found for {selectedUser.Name} {selectedUser.Surname}: ";
                    response.Model = selectedMemberships;
                    return Ok(response);
                } else
                {
                    response.DidError = true;
                    response.Message = $"No house memberships were found for {selectedUser.Name} {selectedUser.Surname}";
                    response.Model = null;
                    return NotFound(response);
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message + "\n" + error.StackTrace;
                response.Model = null;
                return BadRequest(response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] User updateUser, [FromQuery] String clientCode)
        {
            SingleResponse<User> response = new SingleResponse<Models.User>();
            try
            {
                if (clientCode != androidClient)
                {
                    response.DidError = true;
                    response.Message = "Please send valid client credentials to the server";
                    response.Model = null;
                    return BadRequest(response);
                }

                var selectedUser = await userManager.FindByEmailAsync(updateUser.Email);
                if (selectedUser == null)
                {
                    response.DidError = true;
                    response.Message = "No user was found with the supplied email address";
                    response.Model = null;
                    return NotFound(response);
                }
                selectedUser.Name = updateUser.Name;
                selectedUser.Surname = updateUser.Surname;
                selectedUser.SecurityQuestion = updateUser.SecurityQuestion;
                selectedUser.SecurityAnswer = updateUser.SecurityAnswer;
                selectedUser.FirebaseMessagingToken = updateUser.FirebaseMessagingToken;
                var updateResult = await Task.Run(() =>
                {
                    return userRepository.UpdateUserAccount(selectedUser);
                });
                if (updateResult == null)
                {
                    response.DidError = true;
                    response.Model = null;
                    response.Message = "Failed to update the selected user. Please try again";
                    return BadRequest(response);
                } else
                {
                    response.DidError = false;
                    response.Message = "Update was successfull";
                    response.Model = updateResult;
                    return Ok(response);
                }
            } catch (Exception error)
            {
                response.DidError = true;
                response.Message = error.Message + "\n" + error.StackTrace;
                response.Model = null;
                return BadRequest(response);
            }
        }

       
        private String GenerateRandomString()
        {
            Random random = new Random();
            String finalString = "";
            for (int a = 0; a < 10; a++)
            {
                finalString += Convert.ToString(random.Next(1, 50));
            }
            return finalString.Trim();
        }





    }
    
}

