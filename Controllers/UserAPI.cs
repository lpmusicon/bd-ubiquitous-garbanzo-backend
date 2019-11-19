﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackendProject.Interface;
using BackendProject.Helpers;

namespace BackendProject.Controllers
{
    /* DisabledTo nie trzeba wypelniac bedzie dzialac, PWZNumber potrzebny tylko do lekarza
    {
        "Login": "",
        "Password": "",
        "Role": "",
        "DisabledTo": "",
        "Name": "",
        "Lastname": "",
        "PWZNumber": ""
    }
    */
    [ApiController]
    [Route("user/register")]
    public class UserRegisterController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Post([FromForm]RegisterData input)
        {
            using var db = new DatabaseContext();
            // sprawdzenie czy istnieje
            if (db.Users.SingleOrDefault(x => x.Login == input.Login) != null)
                return BadRequest();

            if (input.Login != null &&
                input.Password != null &&
                input.Role != null &&
                input.Name != null &&
                input.Lastname != null &&
                (input.PWZNumber != null && input.PWZNumber.Length == 7 && input.PWZNumber.All(char.IsDigit) ||
                (input.PWZNumber == null && input.Role != "DOCT")))
            {

                var pw = new PasswordHelper();
                var user = new User
                {
                    Login = input.Login,
                    Password = pw.CreateHashedPassword(input.Password),
                    Role = input.Role,
                    DisabledTo = input.DisabledTo
                };

                if (input.Role != "RECP" && input.Role != "LABW" && input.Role != "LABM" && input.Role != "DOCT" && input.Role != "ADMN")
                    return BadRequest(); // jesli zla rola

                db.Users.Add(user);
                db.SaveChanges(); // zapis usera do bazy
                var login_record = db.Users.Single(x => x.Login == input.Login); // tutaj jest record w ktorym jest id tego loginu

                switch (input.Role)
                {
                    case "ADMN":
                        Admin ad = new Admin
                        {
                            AdminId = login_record.UserId,
                            Name = input.Name,
                            Lastname = input.Lastname
                        };
                        db.Admins.Add(ad);
                        break;

                    case "RECP":
                        Receptionist rec = new Receptionist
                        {
                            ReceptionistId = login_record.UserId,
                            Name = input.Name,
                            Lastname = input.Lastname
                        };
                        db.Receptionists.Add(rec);
                        break;

                    case "LABW":
                        LaboratoryWorker lw = new LaboratoryWorker
                        {
                            LaboratoryWorkerId = login_record.UserId,
                            Name = input.Name,
                            Lastname = input.Lastname
                        };
                        db.LaboratoryWorkers.Add(lw);
                        break;

                    case "LABM":
                        LaboratoryManager lm = new LaboratoryManager
                        {
                            LaboratoryManagerId = login_record.UserId,
                            Name = input.Name,
                            Lastname = input.Lastname
                        };
                        db.LaboratoryManagers.Add(lm);
                        break;

                    case "DOCT":
                        Doctor doc = new Doctor
                        {
                            DoctorId = login_record.UserId,
                            Name = input.Name,
                            Lastname = input.Lastname,
                            PWZNumber = input.PWZNumber
                        };
                        db.Doctors.Add(doc);
                        break;
                }
                db.SaveChanges();
                return StatusCode(201); // Created
            }
            return BadRequest("Bad luck");
        }
    }

    /*
    {
        "Login": "",
        "NewDisableTime": ""
    }
    */
    [ApiController]
    [Route("user")]
    public class ChangeDisableToController : ControllerBase
    {
        [HttpPost("{userId}/disable")]
        public IActionResult Post(ChangeDisableDate ndt)
        {
            if (ndt.Login != null && ndt.NewDisableTime != null)
            {
                // Biere login
                using var db = new DatabaseContext();
                var user = db.Users.SingleOrDefault(x => x.Login == ndt.Login);
                if (user != null)
                {
                    user.DisabledTo = ndt.NewDisableTime;
                    db.SaveChanges();
                    return Ok();
                }
                return NotFound();
            }
            return BadRequest();
        }
    }

    /*
    {
        "Login": "",
        "NewPassword": ""
    }
    */
    [ApiController]
    [Authorize]
    [Route("user")]
    public class ChangePasswordController : ControllerBase
    {
        [HttpPost("{userId}/passwd")]
        public IActionResult Post(ChangePassword input)
        {

            if (input.Login != null && input.NewPassword != null)
            {
                // Biere login
                using var db = new DatabaseContext();
                var user = db.Users.SingleOrDefault(x => x.Login == input.Login);
                if (user != null)
                {
                    var pw = new PasswordHelper();
                    string newPassword = pw.CreateHashedPassword(input.NewPassword);

                    user.Password = newPassword;
                    db.SaveChanges();
                    return Ok();
                }
                return NotFound();
            }
            return BadRequest();
        }
    }

    [ApiController]
    [Authorize]
    [Route("user/all")]
    [Route("user")]
    public class UsersController : ControllerBase
    {
        public string Get()
        {
            var result = (from x in new DatabaseContext().Users
                          select new GetUser
                          {
                              UserId = x.UserId,
                              Login = x.Login,
                              Role = x.Role,
                              DisabledTo = x.DisabledTo
                          }).ToList();

            return JsonSerializer.Serialize<List<GetUser>>(result);
        }
    }

    [ApiController]
    [Route("user/roles")]
    public class RoleController : ControllerBase
    {
        public string Get()
        {
            List<Role> roles = new List<Role>() {
                new Role { Mnemo = "ADMN", Name = "Administrator" },
                new Role { Mnemo = "DOCT", Name = "Lekarz" },
                new Role { Mnemo = "RECP", Name = "Recepcjonista" },
                new Role { Mnemo = "LABW", Name = "Pracownik Laboratorium" },
                new Role { Mnemo = "LABM", Name = "Kierownik Laboratorium" }
            };

            return JsonSerializer.Serialize<List<Role>>(roles);
        }
    }

    [ApiController]
    [Route("user/authenticate")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }
        private readonly IUserService _userService;

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Authenticate([FromForm]AuthenticateModel model)
        {
            _logger.LogWarning("ERROR");
            var user = await _userService.Authenticate(model.Login, model.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(user);
        }
    }
}