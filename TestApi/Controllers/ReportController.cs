using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TestApi.Models;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult BillSale(ReportBillSaleModel reportBillSaleModel) {
            try {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT * FROM tb_bill_sale
                    WHERE pay_at BETWEEN @from::DATE AND @to::DATE
                    ORDER BY id DESC
                ";
                cmd.Parameters.AddWithValue("from", reportBillSaleModel.FromDate + " 00:00");
                cmd.Parameters.AddWithValue("to", reportBillSaleModel.ToDate + " 23:59");

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                List<object> list = new List<object>();

                while (reader.Read()) {
                    list.Add(new {
                        id = Convert.ToInt32(reader["id"]),
                        pay_at = Convert.ToDateTime(reader["pay_at"])
                    });
                }
                return Ok(new { results = list });
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }
    }
}
