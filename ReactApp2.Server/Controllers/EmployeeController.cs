using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting; // Ajoutez cette directive using pour IWebHostEnvironment
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using WebAPI.Models;

namespace ReactApp2.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env; // Utilisez IWebHostEnvironment correctement

        public EmployeeController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env; // Initialisez correctement _env ici
        }

        // GET: /employee
        [HttpGet]
        public ActionResult<IEnumerable<Employee>> Get([FromQuery] bool discontinuedOnly = false)
        {
            var employees = new List<Employee>();
            string query = "SELECT EmployeeId, EmployeeName, Department, DateOfJoining, PhotoFileName FROM dbo.Employee";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    using (SqlDataReader myReader = sqlCommand.ExecuteReader())
                    {
                        while (myReader.Read())
                        {
                            employees.Add(new Employee
                            {
                                EmployeeId = (int)myReader["EmployeeId"],
                                EmployeeName = (string)myReader["EmployeeName"],
                                department = (string)myReader["Department"],
                                DateOfJoining = (DateTime)myReader["DateOfJoining"],
                                PhotoFileName = (string)myReader["PhotoFileName"]
                            });
                        }
                    }
                }
            }

            if (discontinuedOnly)
            {
                // Exemple de condition, ajustez en fonction de vos besoins
                employees = employees.Where(e => e.IsDiscontinued).ToList();
            }

            return Ok(employees);
        }
        [HttpPost]
        public IActionResult Post([FromBody] Employee employee)
        {
            if (employee == null || string.IsNullOrEmpty(employee.EmployeeName) || string.IsNullOrEmpty(employee.department))
            {
                return BadRequest(new { Message = "Employee data is invalid. Make sure all required fields are filled." });
            }

            string query = @"
        INSERT INTO dbo.Employee (EmployeeName, Department, DateOfJoining, PhotoFileName)
        OUTPUT INSERTED.EmployeeId, INSERTED.EmployeeName, INSERTED.Department, INSERTED.DateOfJoining, INSERTED.PhotoFileName
        VALUES (@EmployeeName, @Department, @DateOfJoining, @PhotoFileName)";

            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                    sqlCommand.Parameters.AddWithValue("@Department", employee.department); // Corrected case
                    sqlCommand.Parameters.AddWithValue("@DateOfJoining", employee.DateOfJoining);
                    sqlCommand.Parameters.AddWithValue("@PhotoFileName", employee.PhotoFileName);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var newEmployee = new
                            {
                                EmployeeId = reader["EmployeeId"],
                                EmployeeName = reader["EmployeeName"],
                                Department = reader["Department"],
                                DateOfJoining = reader["DateOfJoining"],
                                PhotoFileName = reader["PhotoFileName"]
                            };

                            return Ok(newEmployee);
                        }
                        else
                        {
                            return BadRequest(new { Message = "Error occurred while adding the employee." });
                        }
                    }
                }
            }
        }


        // PUT: /employee/{id}
        [HttpPut("{id}")]
        public IActionResult Put(int id, Employee employee)
        {
            string query = "UPDATE dbo.Employee SET EmployeeName = @EmployeeName, Department = @Department, DateOfJoining = @DateOfJoining, PhotoFileName = @PhotoFileName WHERE EmployeeId = @EmployeeId";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@EmployeeId", id);
                    sqlCommand.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                    sqlCommand.Parameters.AddWithValue("@Department", employee.department);
                    sqlCommand.Parameters.AddWithValue("@DateOfJoining", employee.DateOfJoining);
                    sqlCommand.Parameters.AddWithValue("@PhotoFileName", employee.PhotoFileName);

                    int rowsAffected = sqlCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok(new { Message = "L'employé a été mis à jour avec succès." });
                    }
                    else
                    {
                        return NotFound(new { Message = "Employé non trouvé." });
                    }
                }
            }
        }

        // DELETE: /employee/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            string query = "DELETE FROM dbo.Employee WHERE EmployeeId = @EmployeeId";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@EmployeeId", id);

                    int rowsAffected = sqlCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok(new { Message = "L'employé a été supprimé avec succès." });
                    }
                    else
                    {
                        return NotFound(new { Message = "Employé non trouvé." });
                    }
                }
            }
        }
        [Route("SaveFile")]
        [HttpPost]
        public JsonResult SaveFile()
        {
            try
            {
                var httpRequest = Request.Form;
                var postedFile = httpRequest.Files[0];
                string fileName = postedFile.FileName;

                // Chemin complet pour enregistrer le fichier
                var directoryPath = Path.Combine(_env.ContentRootPath, "Photos");

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var physicalPath = Path.Combine(directoryPath, fileName);

                // Enregistrer le fichier sur le disque
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                // Retourner le nom du fichier enregistré
                return new JsonResult(fileName);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner un fichier par défaut
                return new JsonResult("anonymous.png");
            }
        }

        [Route("GetAllDepartmentNames")]
        public JsonResult GetAllDepartmentNames()
        {
            string query = @"
        select DepartmentName from dbo.Department
    ";

            List<string> departmentNames = new List<string>();
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    using (SqlDataReader myReader = myCommand.ExecuteReader())
                    {
                        while (myReader.Read())
                        {
                            departmentNames.Add(myReader["DepartmentName"].ToString());
                        }
                    }
                }
            }

            return new JsonResult(departmentNames);
        }

    }
}
