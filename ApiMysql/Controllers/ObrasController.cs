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

namespace ApiMysql.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObrasController : ControllerBase
    {
        private readonly MySQLConfiguration _context;

        public ObrasController(MySQLConfiguration context)
        {
            _context = context;
        }

        // GET: api/Obras
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Obras>>> GetObras()
        {
          if (_context.obras == null)
          {
              return NotFound();
          }
            return await _context.obras.ToListAsync();
        }

        // GET: api/Obras/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Obras>> GetObras(int id)
        {
          if (_context.obras == null)
          {
              return NotFound();
          }
            var obras = await _context.obras.FindAsync(id);

            if (obras == null)
            {
                return NotFound();
            }

            return obras;
        }

        // PUT: api/Obras/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutObras(int id, Obras obras)
        {
            if (id != obras.id)
            {
                return BadRequest();
            }

            _context.Entry(obras).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ObrasExists(id))
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

        // POST: api/Obras
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Obras>> PostObras(Obras obras)
        {
          if (_context.obras == null)
          {
              return Problem("Entity set 'MySQLConfiguration.Obras'  is null.");
          }
            _context.obras.Add(obras);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ObrasExists(obras.id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetObras", new { id = obras.id }, obras);
        }

        // DELETE: api/Obras/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObras(int id)
        {
            if (_context.obras == null)
            {
                return NotFound();
            }
            var obras = await _context.obras.FindAsync(id);
            if (obras == null)
            {
                return NotFound();
            }

            _context.obras.Remove(obras);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GESTIÓN DE OBRAS
        // POST: api/Obras/registrarObra
        [HttpPost("registrarObra")]
        [Authorize]
        public async Task<ActionResult> registrarObra([FromBody] Obras obraNueva)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (obraNueva == null)
                {
                    return BadRequest("El objeto obraNueva es nulo");
                }

                // Obtener el id del token de acceso
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userId, out int userIdInt))
                {
                    return BadRequest("El id no es válido"); // Devuelve un código de error adecuado si el id no es un entero válido
                }

                // Asociar el usuario actual con la obra nueva
                obraNueva.user_id = userIdInt;

                await _context.obras.AddAsync(obraNueva);
                await _context.SaveChangesAsync();

                // Si todo se realizó correctamente, confirmar la transacción
                await transaction.CommitAsync();

                return Created("", "La obra se ha registrado correctamente.");
            }
            catch (Exception ex)
            {
                // Si ocurrió algún error, revertir la transacción
                await transaction.RollbackAsync();

                return BadRequest("No se pudo registrar la obra. Error: " + ex.ToString());
            }
        }

        // POST: api/Obras/eliminarObra
        [HttpPost("eliminarObra")]
        [Authorize]
        public async Task<IActionResult> PostEliminarObra([FromBody] EliminarObra request)
        {
            try
            {
                // Buscar la obra por id
                var obra = await _context.obras.FirstOrDefaultAsync(c => c.id == request.id);

                if (obra == null)
                {
                    return BadRequest("No se encontró la obra especificada.");
                }

                // Eliminar la obra encontrado
                _context.obras.Remove(obra);
                await _context.SaveChangesAsync();

                return Created("", "Se eliminó la obra correctamente.");
            }
            catch (Exception ex)
            {
                // Manejar el error de manera adecuada (por ejemplo, registrar el error en un log)

                return BadRequest("No se pudo eliminar la obra.");
            }
        }

        // POST: api/Obras/editarObra
        [HttpPost("editarObra")]
        [Authorize]
        public async Task<ActionResult> editarObra([FromBody] JsonObject json)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var editarObra = JsonSerializer.Deserialize<Obras>(json.ToString(), options);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Buscar la obra por id
                var obra = await _context.obras.FirstOrDefaultAsync(c => c.id == editarObra.id);

                if (obra == null)
                {
                    return BadRequest("No se encontró la obra especificada.");
                }

                // Actualizar los datos de la obra existente con los valores de la obra actualizada
                obra.titulo = editarObra.titulo;
                obra.descripcion = editarObra.descripcion;
                obra.imagen = editarObra.imagen;
                obra.codigo = editarObra.codigo;

                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();

                // Si todo se realizó correctamente, confirmar la transacción
                await transaction.CommitAsync();

                return Created("", "La obra se ha actualizado correctamente.");
            }
            catch (Exception ex)
            {
                // Si ocurrió algún error, revertir la transacción
                await transaction.RollbackAsync();

                return BadRequest("No se pudo actualizar la obra. Error: " + ex.InnerException.Message);
            }
        }

        // GET: api/Obras/listarMisObras
        [HttpGet("listarMisObras")]
        [Authorize]
        public async Task<IActionResult> listarMisObras()
        {
            // Obtener el idUsuario del token de acceso
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out int userIdInt))
            {
                return BadRequest("El idUsuario no es válido"); // Devuelve un código de error adecuado si el idUsuario no es un entero válido
            }

            // Buscar obras por id
            var obras = new List<Obras>();

            obras = await _context.obras
                    .Where(o => o.user_id == userIdInt)
                    .ToListAsync();
            if (obras.Count == 0)
            {
                return Ok("No hay obras que mostrar.");
            }
            return Ok(obras);
        }

        // GET: api/Obras/listarObras
        [HttpGet("listarObras")]
        [Authorize]
        public async Task<IActionResult> listarObras()
        {
            // Buscar obras
            var obras = new List<Obras>();

            obras = await _context.obras.ToListAsync();

            if (obras.Count == 0)
            {
                return Ok("No hay obras que mostrar.");
            }
            return Ok(obras);
        }

        private bool ObrasExists(int id)
        {
            return (_context.obras?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
