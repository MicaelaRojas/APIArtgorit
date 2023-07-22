using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiMysql.Context;
using ApiMysql.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace ApiMysql.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly MySQLConfiguration _context;

        public UsersController(MySQLConfiguration context)
        {
            _context = context;
        }

        private string GetMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Users>>> Getusers()
        {
          if (_context.users == null)
          {
              return NotFound();
          }
            return await _context.users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetUsers(int id)
        {
          if (_context.users == null)
          {
              return NotFound();
          }
            var users = await _context.users.FindAsync(id);

            if (users == null)
            {
                return NotFound();
            }

            return users;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsers(int id, Users users)
        {
            if (id != users.id)
            {
                return BadRequest();
            }

            _context.Entry(users).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Users>> PostUsers(Users users)
        {
          if (_context.users == null)
          {
              return Problem("Entity set 'MySQLConfiguration.users'  is null.");
          }
            _context.users.Add(users);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsers", new { id = users.id }, users);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsers(int id)
        {
            if (_context.users == null)
            {
                return NotFound();
            }
            var users = await _context.users.FindAsync(id);
            if (users == null)
            {
                return NotFound();
            }

            _context.users.Remove(users);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/users/registrarUser
        [HttpPost("registrarUser")]
        public async Task<ActionResult> registrarUser([FromBody] JsonObject json)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var user = JsonSerializer.Deserialize<Users>(json.ToString(), options);

            // Validar si el correo ya existe
            if (await _context.users.AnyAsync(u => u.email == user.email))
            {
                return BadRequest(new { mensaje = "El correo que ha ingresado ya está registrado.", codigo = 400 });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Registrar el usuario
                user.rol = "Usuario";

                // Hashear la password
                user.password = GetMd5Hash(user.password);

                await _context.users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Si todo se realizó correctamente, confirmar la transacción
                await transaction.CommitAsync();

                return Created("", "El usuario se ha registrado correctamente.");
            }
            catch (Exception ex)
            {
                // Si ocurrió algún error, revertir la transacción
                await transaction.RollbackAsync();

                return BadRequest("No se pudo registrar el usuario. Error: " + ex.ToString());
            }
        }

        // POST: api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<string>> IniciarSesionAsync([FromBody] JsonElement json, [FromServices] IConfiguration configuration)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var user = JsonSerializer.Deserialize<Users>(json.GetRawText(), options);

            // Validar si el email existe
            var validation = await _context.users.SingleOrDefaultAsync(u => u.email == user.email);

            // Deshashear la password
            var contrasena = GetMd5Hash(user.password);

            if (validation == null)
            {
                return NotFound("Correo incorrecto");
            }

            // Validar si la contraseña es correcta
            else if (validation.password != contrasena)
            {
                return BadRequest("Contraseña incorrecta");
            }

            // Buscar el usuario en la base de datos
            var userEncontrado = await _context.users.SingleOrDefaultAsync(validation => validation.email.Equals(user.email) && validation.password.Equals(contrasena));

            if (userEncontrado == null)
            {
                return NotFound();
            }

            // Generar el token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["JwtSettings:SecretKey"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userEncontrado.id.ToString()),
                }),
                Expires = DateTime.UtcNow.AddMinutes(15), // Establece la fecha de vencimiento del token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = configuration["JwtSettings:Issuer"],
                Audience = configuration["JwtSettings:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Devolver el token como respuesta
            return Ok(tokenString);
        }

        // POST: api/users/cambiarPassword
        [HttpPost("cambiarPassword")]
        [Authorize]
        public async Task<ActionResult> cambiarPassword([FromBody] CambiarPassword request)
        {
            // Obtener el id del token de acceso
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out int userIdInt))
            {
                return BadRequest("El id no es válido"); // Devuelve un código de error adecuado si el id no es un entero válido
            }

            // Buscar usuario por id
            var userEncontrado = await _context.users.FirstOrDefaultAsync(u => u.id == userIdInt);
            if (userEncontrado == null)
            {
                return NotFound();
            }

            // Deshashear la password actual almacenada en la base de datos
            var contrasenaActual = GetMd5Hash(request.password);

            // Validar que la password enviada coincida con la del usuario encontrado
            if (userEncontrado.password != contrasenaActual)
            {
                return BadRequest("La contraseña actual es incorrecta");
            }

            // Actualizar la contraseña del usuario sin hashearla nuevamente
            userEncontrado.password = GetMd5Hash(request.nueva_password);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool UsersExists(int id)
        {
            return (_context.users?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
