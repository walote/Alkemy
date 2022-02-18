using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace wApi.Controllers
{
    [ApiController]
    [Route("auth/login")]
    public class Login : ControllerBase
    {
        [HttpGet]
        public Models.Response Get([FromHeader] string authorization)
        {
            return Models.Authentication.obtenerToken(authorization);
        }
    }

    [ApiController]
    [Route("auth/register")]
    public class Register : ControllerBase
    {
        [HttpPost]
        async public Task<Models.Response> Get([FromBody] Models.Authentication.Usuario usuario)
        {
            return await Models.Authentication.registrarUsuario(usuario);
        }
    }

    [ApiController]
    [Route("characters")]
    public class Characters : ControllerBase
    {
        [HttpGet("{id?}/{name?}/{age?}/{idMovie?}/{authorization?}")]
        public Models.Response Get(int id, [FromQuery] string name, [FromQuery] int age, [FromQuery] int idMovie, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Personaje.Select(id, name, age, idMovie);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpPost("{authorization?}")]
        public Models.Response Post([FromBody] Models.Personaje personaje, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Personaje.Insert(personaje);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpDelete("{id}/{authorization?}")]
        public Models.Response Delete(int id, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Personaje.Delete(id);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpPut("{authorization?}")]
        public Models.Response Update([FromBody] Models.Personaje personaje, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Personaje.Update(personaje);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }
    }

    [ApiController]
    [Route("movies")]
    public class Movies : ControllerBase
    {
        [HttpGet("{id?}/{name?}/{genre?}/{order?}/{authorization?}")]
        public Models.Response Get(int id, [FromQuery] string name, [FromQuery] int genre, [FromQuery] string order, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Producto.Select(id, name, genre, order);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpPost("{authorization?}")]
        public Models.Response Post([FromBody] Models.Producto producto, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Producto.Insert(producto);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpDelete("{id}/{authorization?}")]
        public Models.Response Delete(int id, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Producto.Delete(id);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }

        [HttpPut("{authorization?}")]
        public Models.Response Update([FromBody] Models.Producto producto, [FromHeader] string authorization)
        {
            if (Models.Authentication.checkToken(authorization))
                return Models.Producto.Update(producto);
            else
                return new Models.Response() { error = "Token no autorizado para operar" };
        }
    }
}
