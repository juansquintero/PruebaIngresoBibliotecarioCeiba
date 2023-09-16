using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaIngresoBibliotecario.Api.Models;
using PruebaIngresoBibliotecario.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PruebaIngresoBibliotecario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrestamoController : ControllerBase
    {
        private readonly PersistenceContext _context;

        public PrestamoController(PersistenceContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CrearPrestamo([FromBody] PrestamoModel prestamo)
        {
            try
            {
                // Realiza las validaciones necesarias aquí
                // Verifica si el usuario invitado ya tiene un préstamo
                var usuarioExistente = _context.Prestamos.FirstOrDefault(p => p.IdentificacionUsuario == prestamo.IdentificacionUsuario);
                if (usuarioExistente != null && usuarioExistente.TipoUsuario == 3)
                {
                    return Ok(new
                    {
                        mensaje = $"El usuario con identificacion {prestamo.IdentificacionUsuario} ya tiene un libro prestado por lo cual no se le puede realizar otro prestamo"
                    });
                }

                // Validaciones y lógica de validación previa

                if (string.IsNullOrEmpty(prestamo.IdentificacionUsuario) || prestamo.IdentificacionUsuario.Length > 10)
                {
                    return BadRequest(new
                    {
                        mensaje = $"El usuario con nombre {prestamo.IdentificacionUsuario} es invalido, trate de nuevo"
                    });
                }

                // Calcula la fecha máxima de devolución según el tipo de usuario
                DateTime fechaMaximaDevolucion;
                switch (prestamo.TipoUsuario)
                {
                    case 1: // Usuario Afiliado
                        fechaMaximaDevolucion = CalculateDueDate(DateTime.Now, 10);
                        break;
                    case 2: // Usuario Empleado
                        fechaMaximaDevolucion = CalculateDueDate(DateTime.Now, 8);
                        break;
                    case 3: // Usuario Invitado
                        fechaMaximaDevolucion = CalculateDueDate(DateTime.Now, 7);
                        break;
                    default:
                        return BadRequest(new
                        {
                            mensaje = "El valor de 'tipoUsuario' debe ser 1, 2 o 3."
                        });
                }

                // Excluye sábados y domingos de la fecha de devolución
                while (fechaMaximaDevolucion.DayOfWeek == DayOfWeek.Saturday || fechaMaximaDevolucion.DayOfWeek == DayOfWeek.Sunday)
                {
                    fechaMaximaDevolucion = fechaMaximaDevolucion.AddDays(1);
                }

                // Asigna la fecha máxima de devolución al préstamo
                prestamo.FechaMaximaDevolucion = fechaMaximaDevolucion;


                // Asigna un nuevo ID (puedes usar Guid.NewGuid())
                prestamo.Id = Guid.NewGuid();

                // Agrega el préstamo a la lista de préstamos
                _context.Prestamos.Add(prestamo);

                // Commit changes asynchronously
                await _context.SaveChangesAsync();

                // Retorna una respuesta exitosa
                return Ok(new
                {
                    id = prestamo.Id,
                    fechaMaximaDevolucion = prestamo.FechaMaximaDevolucion
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the process
                return StatusCode(500, new
                {
                    mensaje = "Ha ocurrido un error al procesar la solicitud: " + ex.Message
                });
            }
        }


        [HttpGet("{idPrestamo}")]
        public async Task<IActionResult> ObtenerPrestamo([FromRoute] Guid idPrestamo)
        {
            try
            {
                // Buscar el préstamo por su ID en la lista (assuming _context.Prestamos is asynchronous)
                var prestamo = await _context.Prestamos.FirstOrDefaultAsync(p => p.Id == idPrestamo);

                if (prestamo == null)
                {
                    return NotFound(new
                    {
                        mensaje = $"El préstamo con ID {idPrestamo} no existe"
                    });
                }

                // Validar los campos antes de responder
                if (!EsTipoUsuarioValido(prestamo.TipoUsuario) || !EsIdentificacionUsuarioValida(prestamo.IdentificacionUsuario))
                {
                    return BadRequest(new
                    {
                        mensaje = "Los campos tipoUsuario, isbn o identificacionUsuario contienen valores no permitidos."
                    });
                }

                // Si todo es válido, responder con el préstamo
                return Ok(new
                {
                    id = prestamo.Id,
                    isbn = prestamo.Isbn,
                    identificacionUsuario = prestamo.IdentificacionUsuario,
                    tipoUsuario = prestamo.TipoUsuario,
                    fechaMaximaDevolucion = prestamo.FechaMaximaDevolucion
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the process
                return StatusCode(500, new
                {
                    mensaje = "Ha ocurrido un error al procesar la solicitud: " + ex.Message
                });
            }
        }


        // Método para validar si el tipo de usuario es válido
        private bool EsTipoUsuarioValido(int tipoUsuario)
        {
            return tipoUsuario >= 1 && tipoUsuario <= 3;

        }

        // Método para validar si la identificación de usuario es válida
        private bool EsIdentificacionUsuarioValida(string identificacionUsuario)
        {
            return !string.IsNullOrEmpty(identificacionUsuario) && identificacionUsuario.Length <= 10;
        }

        private DateTime CalculateDueDate(DateTime startDate, int daysToAdd)
        {
            int daysAdded = 0;
            while (daysAdded < daysToAdd)
            {
                switch (startDate.DayOfWeek)
                {
                    case DayOfWeek.Friday:
                        startDate = startDate.AddDays(3);
                        break;
                    case DayOfWeek.Saturday:
                        startDate = startDate.AddDays(2);
                        break;
                    case DayOfWeek.Sunday:
                        startDate = startDate.AddDays(1);
                        break;
                    default:
                        startDate = startDate.AddDays(1);
                        break;
                }
                daysAdded++;
            }
            return startDate;
        }

    }
}
