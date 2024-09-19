using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WebAPI.Models;

namespace ReactApp2.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class departmentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public departmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<IEnumerable<department>> Get()
        {
            string query = "SELECT DepartmentId, DepartmentName FROM dbo.Department";
            var departments = new List<department>();
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
                            departments.Add(new department
                            {
                                departmentId = (int)myReader["DepartmentId"],
                                departmentName = (string)myReader["DepartmentName"]
                            });
                        }
                    }
                }
            }

            return Ok(departments);
        }

        [HttpPost]
        public IActionResult Post([FromBody] department department)
        {
            if (department == null)
            {
                return BadRequest(new { Message = "Department data is null." });
            }

            if (string.IsNullOrEmpty(department.departmentName))
            {
                return BadRequest(new { Message = "Department name is required." });
            }

            string query = "INSERT INTO dbo.Department (DepartmentName) OUTPUT Inserted.DepartmentId, Inserted.DepartmentName VALUES (@DepartmentName)";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@DepartmentName", department.departmentName);

                    // Exécution de la commande et récupération des données insérées
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Lire les données du département inséré
                            var newDepartment = new
                            {
                                DepartmentId = reader.GetInt32(0),
                                DepartmentName = reader.GetString(1)
                            };
                            return Ok(newDepartment); // Retourner le département ajouté
                        }
                        else
                        {
                            return StatusCode(500, new { Message = "An error occurred while adding the department." });
                        }
                    }
                }
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] department department)
        {
            string query = "UPDATE dbo.Department SET DepartmentName = @DepartmentName WHERE DepartmentId = @DepartmentId";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@DepartmentName", department.departmentName);
                    sqlCommand.Parameters.AddWithValue("@DepartmentId", id);

                    myCon.Open();
                    int rowsAffected = await sqlCommand.ExecuteNonQueryAsync();
                    myCon.Close();

                    if (rowsAffected == 0)
                    {
                        return NotFound();
                    }
                }
            }

            return Ok(new { message = "Department updated successfully." }); // Renvoie une réponse JSON
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            string query = "DELETE FROM dbo.Department WHERE DepartmentId = @DepartmentId";
            string sqlDataSource = _configuration.GetConnectionString("EmployeeAppCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, myCon))
                {
                    sqlCommand.Parameters.AddWithValue("@DepartmentId", id);

                    int rowsAffected = sqlCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok(new { Message = "Department deleted successfully." });
                    }
                    else
                    {
                        return NotFound(new { Message = "Department not found." });
                    }
                }
            }
        }
    }
}



