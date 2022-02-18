using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using System.Net.Http.Headers;

namespace wApi.Models
{
    public class Entorno
    {
        static public string dsn()
        {
            string iRet = null;
            try
            {
                using (var sr = new StreamReader("Entorno.pvt"))
                    iRet = sr.ReadLine();
            }
            catch { }
            return iRet;
        }

        public static string GenerateTokenJwt(string username)
        {
            var secretKey = "this is my custom Secret key for authentication"; // ConfigurationManager.AppSettings["JWT_SECRET_KEY"];
            var audienceToken = "localhost"; // ConfigurationManager.AppSettings["JWT_AUDIENCE_TOKEN"];
            var issuerToken = "localhost"; // ConfigurationManager.AppSettings["JWT_ISSUER_TOKEN"];
            var expireTime = "30"; // ConfigurationManager.AppSettings["JWT_EXPIRE_MINUTES"];

            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(secretKey != null ? secretKey : "SIN_SECRET_KEY"));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) });

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtSecurityToken = tokenHandler.CreateJwtSecurityToken(
                audience: audienceToken,
                issuer: issuerToken,
                subject: claimsIdentity,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(expireTime)),
                signingCredentials: signingCredentials);

            var jwtTokenString = tokenHandler.WriteToken(jwtSecurityToken);
            return jwtTokenString;
        }

        public static async Task sendMail(string tmpTo, string tmpName)
        {
            var client = new SendGridClient(await Entorno.recoverApiKey());
            var from = new EmailAddress("wmet@hotmail.com", "Disney Movies Information Service");
            var subject = "Mensaje de bienvenida a DMIS";
            var to = new EmailAddress(tmpTo, tmpName);
            var plainTextContent = "Ud. se ha registrado exitosamente para utilizar esta API";
            var htmlContent = "<strong>Ud. se ha registrado exitosamente para utilizar esta API</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
        static async private Task<string> recoverApiKey()
        {
            try
            {
                var client = new HttpClient(new HttpClientHandler()) { BaseAddress = new Uri("https://weblogin.muninqn.gov.ar/api/recoverKey") };
                var response = await client.GetAsync(client.BaseAddress);
                if (response.IsSuccessStatusCode)
                {
                    var tmp = response.Content.ReadAsStringAsync().Result.ToString().Substring(1);
                    tmp= tmp.Substring(0, tmp.Length - 1);
                    return tmp;
                } 
            }
            catch { }
            return string.Empty;
        }
    }

    public struct Response
    {
        public object value { get; set; }
        public string error { get; set; }
    }

    public class Personaje
    {
        public class PersonajeReduced
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string imagenUrl { get; set; }
        }

        public int id { get; set; }
        public string nombre { get; set; }
        public int edad { get; set; }
        public float peso { get; set; }
        public string historia { get; set; }
        public string imagenUrl { get; set; }
        public int[] filmografiaID { get; set; }

        static public Response Select(int id, string nombre, int age, int idMovie)
        {
            try
            {
                var iRet = new List<PersonajeReduced>();
                using (var sql = new MySqlConnection(Entorno.dsn()))
                {
                    string where = string.Empty;
                    if (id > 0)
                        where = "WHERE id=" + id;
                    else if (nombre != null)
                        where = "WHERE nombre LIKE '%" + nombre + "%'";
                    else if (age > 0)
                        where = "WHERE edad=" + age;
                    else if (idMovie > 0)
                        where = "WHERE peliculaID=" + idMovie + "";

                    sql.Open();
                    var rdr = new MySqlCommand("SELECT nombre, imagen, id FROM Personajes " + where + " ORDER BY nombre", sql).ExecuteReader();
                    while (rdr.HasRows && rdr.Read())
                        iRet.Add(new PersonajeReduced()
                        {
                            nombre = !rdr.IsDBNull(0) ? rdr.GetString(0).Trim() : "no_name",
                            imagenUrl = !rdr.IsDBNull(1) ? rdr.GetString(1).Trim() : "no_image",
                            id = rdr.GetInt32(2)
                        });
                }

                return new Response() { value = iRet };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Insert(Personaje personaje)
        {
            try
            {
                if (personaje != null)
                {
                    if (personaje.nombre != null && personaje.nombre.Length > 0)
                    {
                        if (personaje.edad > 0)
                        {
                            if (personaje.filmografiaID != null && personaje.filmografiaID.Length > 0)
                            {
                                using (var sql = new MySqlConnection(Entorno.dsn()))
                                {
                                    sql.Open();
                                    var rdr = new MySqlCommand("SELECT id FROM personajes WHERE nombre='" + personaje.nombre + "'", sql).ExecuteReader();
                                    if (rdr.HasRows && rdr.Read())
                                        return new Response() { error = "Sin registrar, ese personaje ya existe con el id " + rdr.GetInt32(0) };
                                    else
                                    {
                                        rdr.Close();
                                        rdr = new MySqlCommand("INSERT INTO personajes (nombre, imagen, edad, peso, historia, peliculaid) VALUES('" + personaje.nombre + "', '" + personaje.imagenUrl + "', " + personaje.edad + ", " + personaje.peso + ", '" + personaje.historia + "', '" + string.Join(",", personaje.filmografiaID.Select(n => n.ToString()).ToArray()) + "'); SELECT id FROM personajes WHERE nombre='" + personaje.nombre + "';", sql).ExecuteReader();
                                        if (rdr.HasRows && rdr.Read())
                                            return new Response() { value = "Registro id: " + rdr.GetInt32(0) };
                                        else
                                            return new Response() { error = "Falla al registrar personaje" };
                                    }
                                }
                            }
                            else
                                return new Response() { error = "Debe completar la lista filmográfica" };
                        }
                        else
                            return new Response() { error = "Debe contener una edad válida" };
                    }
                    else
                        return new Response() { error = "Debe contener un nombre válido" };
                }
                else
                    return new Response() { error = "Información de pelicula incorrecta" };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Delete(int id)
        {
            try
            {
                using (var sql = new MySqlConnection(Entorno.dsn()))
                {
                    sql.Open();
                    if (new MySqlCommand("DELETE FROM personajes WHERE id=" + id, sql).ExecuteNonQuery() > 0)
                        return new Response() { value = "Personaje con id " + id + " eliminado con éxito." };
                    else
                        return new Response() { error = "Falla al intentar eliminar el personaje con id " + id };
                }
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Update(Personaje p)
        {
            try
            {
                if (p.id > 0)
                {
                    using (var sql = new MySqlConnection(Entorno.dsn()))
                    {
                        sql.Open();
                        string upd = p.nombre != null ? "nombre='" + p.nombre + "'" : string.Empty;
                        upd += (p.historia != null ? (upd.Length > 0 ? "," : "") + "historia='" + p.historia + "'" : "");
                        upd += (p.imagenUrl != null ? (upd.Length > 0 ? "," : "") + "imagenUrl='" + p.imagenUrl + "'" : "");
                        upd += (p.filmografiaID != null && p.filmografiaID.Length > 0 ? (upd.Length > 0 ? "," : "") + "PeliculaID='" + string.Join(",", p.filmografiaID.Select(n => n.ToString()).ToArray()) + "'" : "");
                        upd += (p.edad > 0 ? (upd.Length > 0 ? "," : "") + "edad=" + p.edad : "");
                        upd += (p.peso > 0 ? (upd.Length > 0 ? "," : "") + "peso=" + p.peso : "");

                        if (new MySqlCommand("UPDATE personajes SET " + upd + " WHERE id=" + p.id, sql).ExecuteNonQuery() > 0)
                            return new Response() { value = "Personaje con id " + p.id + " actualizado con éxito" };
                        else
                            return new Response() { error = "Falla al actualizar personaje con registro id " + p.id };
                    }
                }
                else
                    return new Response() { error = "Falla al actualizar el personaje, no se ha indicado id" };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
    }

    public class Producto
    {
        public class Genero
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public int[] productos { get; set; }
        }

        public int id { get; set; }
        public string titulo { get; set; }
        public DateTime fechaCreacion { get; set; }
        public int calificacion;
        public string imagenUrl { get; set; }
        public int[] personajes { get; set; }

        static public Response Select(int id, string nombre, int genre, string order)
        {
            try
            {
                var iRet = new List<Producto>();
                using (var sql = new MySqlConnection(Entorno.dsn()))
                {
                    sql.Open();
                    string where = string.Empty;
                    order = order != null && (order.ToUpper() == "ASC" || order.ToUpper() == "DESC") ? order : string.Empty;
                    string join = string.Empty;
                    MySqlDataReader rdr = null;

                    if (genre > 0)
                    {
                        string inClause = null;
                        rdr = new MySqlCommand("SELECT nombre, imagen, peliculasid FROM genero WHERE id=" + genre, sql).ExecuteReader();
                        if (rdr.HasRows && rdr.Read() && !rdr.IsDBNull(2))
                            foreach (var i in rdr.GetString(2).Trim().Split(','))
                                inClause += (inClause != null ? "," : "") + i;

                        where = "WHERE id IN(" + inClause + ") ORDER BY titulo" + order;
                        rdr.Close();
                    }
                    else if (id > 0)
                        where = "WHERE a.id=" + id;
                    else if (nombre != null)
                        where = "WHERE a.titulo LIKE '%" + nombre + "%' ORDER BY a.titulo " + order;

                    rdr = new MySqlCommand("SELECT a.titulo, a.imagen, a.fechaCreacion, a.id FROM pelicula AS a " + where, sql).ExecuteReader();
                    while (rdr.HasRows && rdr.Read())
                        iRet.Add(new Producto()
                        {
                            titulo = !rdr.IsDBNull(0) ? rdr.GetString(0).Trim() : "no_name",
                            imagenUrl = !rdr.IsDBNull(1) ? rdr.GetString(1).Trim() : "no_image",
                            fechaCreacion = !rdr.IsDBNull(2) ? rdr.GetDateTime(2) : new DateTime(),
                            id = rdr.GetInt32(3)
                        });

                }

                return new Response() { value = iRet };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Insert(Producto producto)
        {
            try
            {
                if (producto != null)
                {
                    if (producto.titulo != null && producto.titulo.Length > 0)
                    {
                        if (producto.fechaCreacion != null && producto.fechaCreacion.Year > 1)
                        {
                            if (producto.personajes != null && producto.personajes.Length > 0)
                            {
                                using (var sql = new MySqlConnection(Entorno.dsn()))
                                {
                                    sql.Open();
                                    var rdr = new MySqlCommand("SELECT id FROM pelicula WHERE titulo='" + producto.titulo + "'", sql).ExecuteReader();
                                    if (rdr.HasRows && rdr.Read())
                                        return new Response() { error = "Sin registrar, esa pelicula ya existe con el id " + rdr.GetInt32(0) };
                                    else
                                    {
                                        rdr.Close();
                                        rdr = new MySqlCommand("INSERT INTO pelicula (imagen, titulo, fechaCreacion, calificacion, personajesID) VALUES('" + producto.imagenUrl + "', '" + producto.titulo + "', '" + producto.fechaCreacion.ToString("yyyy/MM/dd HH:mm:ss") + "', " + producto.calificacion + ", '" + string.Join(",", producto.personajes.Select(n => n.ToString()).ToArray()) + "'); SELECT id FROM pelicula WHERE titulo='" + producto.titulo + "';", sql).ExecuteReader();
                                        if (rdr.HasRows && rdr.Read())
                                            return new Response() { value = "Registro id: " + rdr.GetInt32(0) };
                                        else
                                            return new Response() { error = "Falla al registrar pelicula" };
                                    }
                                }
                            }
                            else
                                return new Response() { error = "Debe completar la lista de personajes" };
                        }
                        else
                            return new Response() { error = "Debe contener una fecha de creación válida" };
                    }
                    else
                        return new Response() { error = "Debe contener un titulo válido" };
                }
                else
                    return new Response() { error = "Información de pelicula incorrecta" };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Delete(int id)
        {
            try
            {
                using (var sql = new MySqlConnection(Entorno.dsn()))
                {
                    sql.Open();
                    if (new MySqlCommand("DELETE FROM pelicula WHERE id=" + id, sql).ExecuteNonQuery() > 0)
                        return new Response() { value = "Pelicula con id " + id + " eliminada con éxito." };
                    else
                        return new Response() { error = "Falla al intentar eliminar la pelicula con id " + id };
                }
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
        static public Response Update(Producto p)
        {
            try
            {
                using (var sql = new MySqlConnection(Entorno.dsn()))
                {
                    sql.Open();
                    string upd = p.titulo != null ? "titulo='" + p.titulo + "'" : string.Empty;
                    upd += (p.fechaCreacion != null && p.fechaCreacion.Year > 1 ? (upd.Length > 0 ? "," : "") + "fechaCreacion='" + p.fechaCreacion.ToString("yyyy/MM/dd HH:mm:ss") + "'" : "");
                    upd += (p.imagenUrl != null ? (upd.Length > 0 ? "," : "") + "imagenUrl='" + p.imagenUrl + "'" : "");
                    upd += (p.personajes != null && p.personajes.Length > 0 ? (upd.Length > 0 ? "," : "") + "personajesID='" + string.Join(",", p.personajes.Select(n => n.ToString()).ToArray()) + "'" : "");

                    if (new MySqlCommand("UPDATE pelicula SET " + upd + ", calificacion='" + p.calificacion + "' WHERE id=" + p.id, sql).ExecuteNonQuery() > 0)
                        return new Response() { value = "Pelicula con id " + p.id + " actualizada con éxito" };
                    else
                        return new Response() { error = "Falla al actualizar pelicula con registro id " + p.id };
                }
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }
    }

    public class Genero
    {
        public string nombre;
        public string imagenUrl;
        public List<Producto> filmografia = new List<Producto>();
    }

    public class Authentication
    {
        public class Usuario
        {
            public string nombre { get; set; }
            public string clave { get; set; }
            public string email { get; set; }
        }

        static public Response obtenerToken(string authCode)
        {
            try
            {
                if (authCode != null && authCode.Length > 0 && authCode.StartsWith("Basic"))
                {
                    string tmp = Encoding.GetEncoding("iso-8859-1").GetString(Convert.FromBase64String(authCode.Substring(6).Trim()));
                    var credenciales = tmp.Split(':');

                    string user = null;
                    string pass = null;
                    if (credenciales.Count() == 2)
                    {
                        user = credenciales[0].Trim();
                        pass = credenciales[1].Trim();
                    }

                    if (user != null && user.Length > 0)
                    {
                        if (pass != null && pass.Length > 0)
                        {
                            using (var sql = new MySqlConnection(Entorno.dsn()))
                            {
                                sql.Open();
                                var rdr = new MySqlCommand("SELECT id FROM usuarios WHERE usuario='" + user + "' AND clave='" + pass + "'", sql).ExecuteReader();
                                if (rdr.HasRows && rdr.Read() && !rdr.IsDBNull(0) && rdr.GetInt32(0) > 0)
                                {
                                    int userID = rdr.GetInt32(0);
                                    rdr.Close();

                                    var jwt = Entorno.GenerateTokenJwt(user);
                                    new MySqlCommand("UPDATE usuarios SET token='" + jwt + "' WHERE id=" + userID, sql).ExecuteNonQuery();

                                    return new Response() { value = jwt };
                                }
                                else
                                    return new Response() { error = "Usuario y clave no coinciden, reintente" };
                            }
                        }
                        else
                            return new Response() { error = "Debe especificar clave en el header" };
                    }
                    else
                        return new Response() { error = "Debe especificar usuario en el header" };
                }
                else
                    return new Response() { error = "Debe authorizar usuario y clave en el header" };
            }
            catch (Exception ex)
            {
                return new Response() { error = ex.Message };
            }
        }

        async static public Task<Response> registrarUsuario(Usuario usr)
        {
            try
            {
                if (usr != null)
                {
                    if (usr.nombre != null && usr.nombre.Length > 0 && usr.email != null && usr.email.Length > 0 && usr.clave != null && usr.clave.Length > 0)
                    {
                        using (var sql = new MySqlConnection(Entorno.dsn()))
                        {
                            sql.Open();
                            var rdr = new MySqlCommand("SELECT id FROM usuarios WHERE usuario = '" + usr.email + "'", sql).ExecuteReader();
                            if (rdr.HasRows && rdr.Read())
                                return new Response() { error = "Ya existe un registro con ese email, utilice otra dirección de correo electrónico" };
                            else
                            {
                                rdr.Close();

                                var jwt = Entorno.GenerateTokenJwt(usr.email);
                                new MySqlCommand("INSERT INTO usuarios (nombre, usuario, clave, token) VALUES('" + usr.nombre + "', '" + usr.email + "', '" + usr.clave + "', '" + jwt + "');", sql).ExecuteNonQuery();

                                await Models.Entorno.sendMail(usr.email, usr.nombre);

                                return new Response() { value = jwt };
                            }
                        }
                    }
                    else
                        return new Response() { error = "Ningún elemento del Request puede ser nulo" };
                }
                else
                    return new Response() { error = "Cuerpo de Request inválido" };
            }
            catch (Exception ex) { return new Response() { error = ex.Message }; }
        }

        static public bool checkToken(string token)
        {
            bool iRet = false;
            try
            {
                if (token != null && token.ToLower().StartsWith("bearer"))
                {
                    token = token.Substring(6).Trim();
                    using (var sql = new MySqlConnection(Entorno.dsn()))
                    {
                        sql.Open();
                        var rdr = new MySqlCommand("SELECT id FROM usuarios WHERE token='" + token + "'", sql).ExecuteReader();
                        iRet = rdr.HasRows && rdr.Read() && rdr.GetInt32(0) > 0;
                    }
                }
            }
            catch { }
            return iRet;
        }
    }
}
